using System;
using System.Net.Quic;
using OoLunar.Willow.Models;
using OoLunar.Willow.Models.Actions;

namespace OoLunar.Willow.Server.Models.Actions
{
    public interface IServerAction : IAction
    {
        void InjectDependencies(UserModel currentUser, QuicConnection connection, QuicStream stream, IServiceProvider serviceProvider);
    }
}
