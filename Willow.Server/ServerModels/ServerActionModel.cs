using OoLunar.Willow.Models;
using OoLunar.Willow.Server.Models.Actions;

namespace OoLunar.Willow.Server.Models
{
    public sealed class ServerActionModel : ActionModel
    {
        public new IServerAction? Data { get; init; }

        public ServerActionModel(ActionFlags action, object? data = null) : base(action, data) { }
    }
}
