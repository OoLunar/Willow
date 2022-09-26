using System;
using System.Net;

namespace OoLunar.Willow.Database.Models
{
    public sealed class LoginModel
    {
        public Ulid? Id { get; init; }
        public string? UserAgent { get; init; }
        public IPEndPoint RemoteEndPoint { get; init; }
        public DateTimeOffset AttemptedAt { get; init; }
        public bool Successful { get; init; }

        public LoginModel(Ulid? id, string? userAgent, IPEndPoint remoteEndPoint, DateTimeOffset attemptedAt, bool successful)
        {
            Id = id;
            UserAgent = userAgent;
            RemoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
            AttemptedAt = attemptedAt;
            Successful = successful;
        }
    }
}
