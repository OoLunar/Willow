using System;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OoLunar.Willow.Models;
using OoLunar.Willow.Models.Actions;
using OoLunar.Willow.Payloads;

namespace OoLunar.Willow.Server.Models.Actions
{
    public sealed class AlterSettingsServerAction : AlterSettingsAction, IServerAction
    {
        private UserModel _currentUser = null!;
        private DatabaseContext _database = null!;

        public AlterSettingsServerAction(UserModel newModel) : base(newModel) { }

        public void InjectDependencies(UserModel currentUser, QuicConnection connection, IServiceProvider serviceProvider)
        {
            _currentUser = currentUser;
            _database = serviceProvider.GetRequiredService<DatabaseContext>();
        }

        public async Task<object?> ExecuteAsync(Ulid correlationId, CancellationToken cancellationToken = default)
        {
            if (_currentUser.Id != NewModel.Id && !_currentUser.Flags.HasFlag(UserFlags.Admin))
            {
                return new ErrorPayload(ErrorCode.Unauthorized, "Lack of admin permissions");
            }

            UserModel? alteredUser = await _database.Users.FirstOrDefaultAsync(user => user.Id == NewModel.Id, CancellationToken.None);
            if (alteredUser == null)
            {
                return new ErrorPayload(ErrorCode.InvalidData, $"User {NewModel.Id} does not exist");
            }
            else if (!_currentUser.Flags.HasFlag(UserFlags.Admin) && alteredUser.Flags.HasFlag(UserFlags.Admin))
            {
                return new ErrorPayload(ErrorCode.Unauthorized, "Lack of admin permissions");
            }
            else if (!alteredUser.TrySetDisplayName(NewModel.DisplayName, out string? errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage);
            }
            else if (!alteredUser.TrySetPasswordHash(NewModel.PasswordHash, out errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage);
            }
            else if (!alteredUser.TrySetFlags(NewModel.Flags, out errorMessage))
            {
                return new ErrorPayload(ErrorCode.InvalidData, errorMessage);
            }

            await _database.SaveChangesAsync(CancellationToken.None);
            return alteredUser;
        }
    }
}
