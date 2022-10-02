using System;
using System.Collections.Generic;

namespace OoLunar.Willow.Models.Actions
{
    public class ExecuteCommandAction
    {
        public Ulid CommandId { get; init; }
        public Dictionary<string, string>? Arguments { get; init; }

        public ExecuteCommandAction(Ulid commandId, Dictionary<string, string>? arguments)
        {
            CommandId = commandId;
            Arguments = arguments;
        }
    }
}
