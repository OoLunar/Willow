using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Humanizer;

namespace OoLunar.Willow.Models
{
    public class CommandModel
    {
        public Ulid UserId { get; init; }
        public Ulid CommandId { get; init; }
        public CommandFlags Flags { get; private set; }
        public string DisplayName { get; private set; }
        public string? Description { get; private set; }
        public string Command { get; private set; }
        public string WorkingDirectory { get; private set; }

        public CommandModel(Ulid userId, CommandFlags flags, string command, string? displayName, string? description, string? workingDirectory)
        {
            UserId = userId;
            CommandId = Ulid.NewUlid();
            Flags = flags;
            Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentNullException(nameof(command)) : command;
            DisplayName = displayName ?? command.Split(' ')[0].Titleize();
            Description = description;
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        }

        public bool TrySetFlags(CommandFlags commandFlags, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;
            Flags = commandFlags;
            return true;
        }

        public bool TrySetDisplayName(string newDisplayName, [NotNullWhen(false)] out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(newDisplayName))
            {
                errorMessage = "DisplayName cannot be null or whitespace.";
                return false;
            }

            errorMessage = null;
            DisplayName = newDisplayName;
            return true;
        }

        public bool TrySetDescription(string? newDescription, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;
            Description = newDescription;
            return true;
        }

        public bool TrySetCommand(string newCommand, [NotNullWhen(false)] out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(newCommand))
            {
                errorMessage = "DisplayName cannot be null or whitespace.";
                return false;
            }

            IEnumerable<string> splitPath = Environment.GetEnvironmentVariable("PATH")?.Split(':') ?? Environment.OSVersion.Platform switch
            {
                PlatformID.Unix => new[] { "/usr/local/bin", "/usr/local/sbin", "/usr/bin", "/usr/sbin", "/bin", "/sbin" },
                _ => Array.Empty<string>()
            };

            if (!splitPath.Contains(newCommand))
            {
                errorMessage = $"{newCommand} was not found in system $PATH";
                return false;
            }

            errorMessage = null;
            Command = newCommand;
            return true;
        }

        public bool TrySetWorkingDirectory(string newWorkingDirectory, [NotNullWhen(false)] out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(newWorkingDirectory))
            {
                errorMessage = "DisplayName cannot be null or whitespace.";
                return false;
            }
            else if (!Directory.Exists(newWorkingDirectory))
            {
                errorMessage = $"Directory {newWorkingDirectory} doesn't exist.";
                return false;
            }
            errorMessage = null;
            WorkingDirectory = newWorkingDirectory;
            return true;
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
