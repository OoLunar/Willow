using System;
using System.Net.Quic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OoLunar.Willow.Models;
using OoLunar.Willow.Models.Actions;
using OoLunar.Willow.Payloads;

namespace OoLunar.Willow.Server.Actions
{
    public sealed class AlterSettingsServerAction : AlterSettingsAction, IServerAction
    {
        private UserModel _currentUser = null!;
        private DatabaseContext _database = null!;
        private QuicStream _stream = null!;

        public AlterSettingsServerAction(UserModel newModel) : base(newModel) { }

        public Task InjectDependencies(UserModel currentUser, QuicConnection connection, QuicStream stream, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _currentUser = currentUser;
            _stream = stream;
            _database = serviceProvider.GetRequiredService<DatabaseContext>();
            return Task.CompletedTask;
        }

        public override async Task Execute()
        {
            if (_currentUser.Id != NewModel.Id && !NewModel.Flags.HasFlag(UserFlags.Admin))
            {
                await JsonSerializer.SerializeAsync(_stream, new ErrorPayload(ErrorCode.Unauthorized, "Lack of admin permissions"), cancellationToken: CancellationToken.None);
                return;
            }
            else if (_currentUser.Flags.HasFlag(UserFlags.Admin) && !NewModel.Flags.HasFlag(UserFlags.Admin))
            {
                await JsonSerializer.SerializeAsync(_stream, new ErrorPayload(ErrorCode.Unauthorized, "Lack of admin permissions"), cancellationToken: CancellationToken.None);
                return;
            }

            UserModel? alteredUser = await _database.Users.FirstOrDefaultAsync(user => user.Id == _currentUser.Id, CancellationToken.None);
            if (alteredUser == null)
            {
                await JsonSerializer.SerializeAsync(_stream, new ErrorPayload(ErrorCode.InvalidData, $"User {_currentUser.Id} does not exist"), cancellationToken: CancellationToken.None);
                return;
            }
            else if (!alteredUser.TrySetDisplayName(_currentUser.DisplayName, out string? errorMessage))
            {
                await JsonSerializer.SerializeAsync(_stream, new ErrorPayload(ErrorCode.InvalidData, errorMessage), cancellationToken: CancellationToken.None);
                return;
            }
            else if (!alteredUser.TrySetPasswordHash(_currentUser.PasswordHash, out errorMessage))
            {
                await JsonSerializer.SerializeAsync(_stream, new ErrorPayload(ErrorCode.InvalidData, errorMessage), cancellationToken: CancellationToken.None);
                return;
            }
            else if (!alteredUser.TrySetFlags(_currentUser.Flags, out errorMessage))
            {
                await JsonSerializer.SerializeAsync(_stream, new ErrorPayload(ErrorCode.InvalidData, errorMessage), cancellationToken: CancellationToken.None);
                return;
            }

            await _database.SaveChangesAsync(CancellationToken.None);
            await JsonSerializer.SerializeAsync(_stream, alteredUser, cancellationToken: CancellationToken.None);
        }
    }
}
