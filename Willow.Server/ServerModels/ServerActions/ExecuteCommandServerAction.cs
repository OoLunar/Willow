using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OoLunar.Willow.Models;
using OoLunar.Willow.Models.Actions;
using OoLunar.Willow.Payloads;

namespace OoLunar.Willow.Server.Models.Actions
{
    public sealed class ExecuteCommandServerAction : ExecuteCommandAction, IServerAction
    {
        private UserModel _currentUser = null!;
        private QuicConnection _connection = null!;
        private DatabaseContext _database = null!;

        public ExecuteCommandServerAction(Ulid commandId, Dictionary<string, string>? arguments) : base(commandId, arguments) { }

        public void InjectDependencies(UserModel currentUser, QuicConnection connection, IServiceProvider serviceProvider)
        {
            _currentUser = currentUser;
            _connection = connection;
            _database = serviceProvider.GetRequiredService<DatabaseContext>();
        }

        public async Task<object?> ExecuteAsync(Ulid correlationId, CancellationToken cancellationToken = default)
        {
            CommandModel? command = await _database.Commands.FirstOrDefaultAsync(command => command.CommandId == CommandId && command.UserId == _currentUser.Id, CancellationToken.None);
            if (command == null)
            {
                return new ErrorPayload(ErrorCode.InvalidData, $"Command {CommandId} does not exist");
            }

            Process process = new()
            {
                EnableRaisingEvents = true,
                // Select->Aggregate has poor performance with strings, however we don't expect many arguments for this to matter
                StartInfo = new(command.Command, Arguments?.Select(arg => $"--{arg.Key}=\"{arg.Value}\"").Aggregate((a, b) => $"{a} {b}") ?? string.Empty)
                {
                    WorkingDirectory = command.WorkingDirectory,
                    RedirectStandardOutput = command.Flags.HasFlag(CommandFlags.SendOutput),
                    RedirectStandardError = command.Flags.HasFlag(CommandFlags.SendOutput),
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (command.Flags.HasFlag(CommandFlags.SendOutput))
            {
                Task task = Task.Run(async () =>
                {
                    QuicStream quicStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, cancellationTokenSource.Token);
                    process.OutputDataReceived += async (sender, data) => await CopyToStreamAsync(quicStream, data.Data, cancellationToken);
                    process.ErrorDataReceived += async (sender, data) => await CopyToStreamAsync(quicStream, data.Data, cancellationToken);

                    while (!cancellationTokenSource.IsCancellationRequested)
                    {
                        byte[] buffer = new byte[1024];
                        await quicStream.ReadAsync(buffer, cancellationTokenSource.Token);
                        await process.StandardInput.WriteAsync(Encoding.UTF8.GetString(buffer));
                    }
                }, cancellationTokenSource.Token);
            }

            process.Start();
            await process.WaitForExitAsync(cancellationToken);
            cancellationTokenSource.Cancel(false);
            return new CommandResultPayload
            {
                CommandId = command.CommandId,
                CorrelationId = correlationId,
                ExitCode = process.ExitCode,
                StartedAt = process.StartTime,
                FinishedAt = process.ExitTime
            };
        }

        private static ValueTask CopyToStreamAsync(Stream stream, string? args, CancellationToken cancellationToken = default) => stream == null
            ? throw new InvalidOperationException("Stream is null")
            : stream.WriteAsync(Encoding.UTF8.GetBytes(args ?? string.Empty), cancellationToken);
    }
}
