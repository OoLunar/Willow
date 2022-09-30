using System;
using System.Net.Quic;
using System.Threading;
using System.Threading.Tasks;
using OoLunar.Willow.Models;

namespace OoLunar.Willow.Server.Actions
{
    public interface IServerAction
    {
        Task InjectDependencies(UserModel currentUser, QuicConnection connection, QuicStream stream, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }
}
