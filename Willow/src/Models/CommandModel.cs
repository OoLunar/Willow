using System;
using Humanizer;

namespace OoLunar.Willow.Models
{
    public class CommandModel
    {
        public Ulid UserId { get; init; }
        public Ulid CommandId { get; init; }
        public CommandFlags Flags { get; init; }
        public string? DisplayName { get; init; }
        public string? Description { get; init; }
        public string Command { get; init; }
        public string? WorkingDirectory { get; init; }

        public CommandModel(Ulid userId, CommandFlags flags, string command, string? displayName, string? description, string? workingDirectory)
        {
            UserId = userId;
            CommandId = Ulid.NewUlid();
            Flags = flags;
            Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentNullException(nameof(command)) : command;
            DisplayName = displayName ?? command.Split(' ')[0].Titleize();
            Description = description;
            WorkingDirectory = workingDirectory;
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
