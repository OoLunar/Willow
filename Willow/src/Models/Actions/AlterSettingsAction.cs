using System;
using System.Threading.Tasks;

namespace OoLunar.Willow.Models.Actions
{
    public class AlterSettingsAction : IAction
    {
        public Ulid CorrelationId { get; init; }
        public UserModel NewModel { get; init; }

        public AlterSettingsAction(UserModel newModel)
        {
            CorrelationId = Ulid.NewUlid();
            NewModel = newModel;
        }

        public virtual Task Execute() => throw new NotImplementedException();
    }
}
