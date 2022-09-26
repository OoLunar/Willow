using System;
using System.Collections.Generic;

namespace OoLunar.Willow.Database.Models
{
    public sealed class ExecuteCommandModel
    {
        public Ulid CommandId { get; init; }
        public Dictionary<string, string>? Arguments { get; init; }
    }
}
