using System;

namespace OoLunar.Willow.Payloads
{
    public sealed class CommandResultPayload
    {
        public Ulid CommandId { get; init; }
        public Ulid CorrelationId { get; init; }
        public int ExitCode { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset FinishedAt { get; init; }
    }
}
