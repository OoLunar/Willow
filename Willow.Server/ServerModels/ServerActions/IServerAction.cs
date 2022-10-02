using System;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using OoLunar.Willow.Models;

namespace OoLunar.Willow.Server.Models.Actions
{
    public interface IServerAction
    {
        void InjectDependencies(UserModel currentUser, QuicConnection connection, IServiceProvider serviceProvider);
        Task<object?> ExecuteAsync(Ulid correlationId, CancellationToken cancellationToken = default);
    }
}
