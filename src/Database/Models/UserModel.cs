using System;

namespace OoLunar.Willow.Database.Models
{
    public sealed class UserModel
    {
        public Ulid Id { get; init; }
        public string DisplayName { get; init; }
        public string PasswordHash { get; init; }

        public UserModel(Ulid id, string displayName, string passwordHash)
        {
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentNullException(nameof(displayName)) : displayName;
            PasswordHash = passwordHash;
        }
    }
}
