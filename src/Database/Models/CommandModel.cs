using System;
using Humanizer;

namespace OoLunar.Willow.Database.Models
{
    public sealed class CommandModel
    {
        public Ulid CommandId { get; init; }
        public CommandFlags Flags { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
        public string Command { get; init; }

        public CommandModel(CommandFlags flags, string command, string? displayName, string? description)
        {
            CommandId = Ulid.NewUlid();
            Flags = flags;
            Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentNullException(nameof(command)) : command;
            DisplayName = displayName ?? command.Split(' ')[0].Titleize();
            Description = description;
        }
    }

    [Flags]
    public enum CommandFlags
    {
        None = 0,
        Enabled = 1,
        SendOutput = 2,
    }
}
