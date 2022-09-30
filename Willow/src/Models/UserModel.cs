using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace OoLunar.Willow.Models
{
    public class UserModel : ICloneable
    {
        public Ulid Id { get; init; }
        public string DisplayName { get; private set; }
        public string PasswordHash { get; private set; }
        public CultureInfo Culture { get; private set; }
        public UserFlags Flags { get; private set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public UserModel(Ulid id, string displayName, string passwordHash, CultureInfo cultureInfo, UserFlags userFlags)
        {
            Id = id;
            if (!TrySetDisplayName(displayName, out string? errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(displayName));
            }
            else if (!TrySetPasswordHash(passwordHash, out errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(passwordHash));
            }
            else if (!TrySetCulture(cultureInfo, out errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(cultureInfo));
            }
            else if (!TrySetFlags(userFlags, out errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(userFlags));
            }
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public bool TrySetDisplayName(string displayName, [NotNullWhen(false)] out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                errorMessage = "Display name cannot be null or whitespace.";
                return false;
            }

            errorMessage = null;
            DisplayName = displayName;
            return true;
        }


        public bool TrySetPasswordHash(string passwordHash, [NotNullWhen(false)] out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
            {
                errorMessage = "Password hash cannot be null or whitespace.";
                return false;
            }

            errorMessage = null;
            PasswordHash = passwordHash;
            return true;
        }

        public bool TrySetCulture(CultureInfo cultureInfo, [NotNullWhen(false)] out string? errorMessage)
        {
            if (cultureInfo is null)
            {
                errorMessage = "Culture cannot be null.";
                return false;
            }

            errorMessage = null;
            Culture = cultureInfo;
            return true;
        }

        public bool TrySetFlags(UserFlags flags, [NotNullWhen(false)] out string? errorMessage)
        {
            errorMessage = null;
            Flags = flags;
            return true;
        }

        public object Clone() => new UserModel(Id, DisplayName, PasswordHash, Culture, Flags);
    }

    [Flags]
    public enum UserFlags
    {
        Disabled,
        Admin
    }
}
