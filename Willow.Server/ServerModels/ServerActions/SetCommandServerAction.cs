using System;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OoLunar.Willow.Models;
using OoLunar.Willow.Payloads;

namespace OoLunar.Willow.Server.Models.Actions
{
    public sealed class SetCommandServerAction : CommandModel, IServerAction
    {
        private UserModel _currentUser = null!;
        private DatabaseContext _database = null!;

        public SetCommandServerAction(Ulid userId, CommandFlags flags, string command, string? displayName, string? description, string? workingDirectory) : base(userId, flags, command, displayName, description, workingDirectory) { }

        public void InjectDependencies(UserModel currentUser, QuicConnection connection, IServiceProvider serviceProvider)
        {
            _currentUser = currentUser;
            _database = serviceProvider.GetRequiredService<DatabaseContext>();
        }

        public async Task<object?> ExecuteAsync(Ulid correlationId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.Id != UserId && !_currentUser.Flags.HasFlag(UserFlags.Admin))
            {
                return new ErrorPayload(ErrorCode.Unauthorized, "Lack of admin permissions");
            }

            UserModel? alteredUser = await _database.Users.FirstOrDefaultAsync(user => user.Id == UserId, CancellationToken.None);
            if (alteredUser == null)
            {
                return new ErrorPayload(ErrorCode.InvalidData, $"User {UserId} does not exist");
            }
            else if (!_currentUser.Flags.HasFlag(UserFlags.Admin) && alteredUser.Flags.HasFlag(UserFlags.Admin))
            {
                return new ErrorPayload(ErrorCode.Unauthorized, "Lack of admin permissions");
            }

            CommandModel? command = await _database.Commands.FirstOrDefaultAsync(command => command.UserId == UserId, CancellationToken.None);
            if (command == null)
            {
                _database.Commands.Add(this);
                await _database.SaveChangesAsync(CancellationToken.None);
                return this;
            }
            else if (command.TrySetCommand(Command, out string? errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage, this);
            }
            else if (command.TrySetDescription(Description, out errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage, this);
            }
            else if (command.TrySetDisplayName(DisplayName, out errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage, this);
            }
            else if (command.TrySetFlags(Flags, out errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage, this);
            }
            else if (command.TrySetWorkingDirectory(WorkingDirectory, out errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage, this);
            }

            _database.Commands.Add(command);
            await _database.SaveChangesAsync(CancellationToken.None);
            return this;
        }
    }
}
