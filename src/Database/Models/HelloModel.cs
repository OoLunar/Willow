using System;

namespace OoLunar.Willow.Database.Models
{
    public sealed class HelloModel
    {
        public Ulid Id { get; init; }
        public string PasswordHash { get; init; }
        public string? UserAgent { get; init; }

        public HelloModel(Ulid id, string passwordHash, string userAgent)
        {
            Id = id;
            PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? throw new ArgumentNullException(nameof(passwordHash)) : passwordHash;
            UserAgent = userAgent;
        }
    }
}
