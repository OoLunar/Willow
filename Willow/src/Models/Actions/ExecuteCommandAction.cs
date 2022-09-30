using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OoLunar.Willow.Models.Actions
{
    public class ExecuteCommandAction : IAction
    {
        public Ulid CorrelationId { get; init; }
        public Ulid CommandId { get; init; }
        public Dictionary<string, string>? Arguments { get; init; }

        public ExecuteCommandAction(Ulid commandId, Dictionary<string, string>? arguments)
        {
            CorrelationId = Ulid.NewUlid();
            CommandId = commandId;
            Arguments = arguments;
        }

        public virtual Task Execute() => throw new NotImplementedException();
    }
}
