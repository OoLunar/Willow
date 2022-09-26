using System.Reflection;

namespace OoLunar.Willow.Net.Payloads
{
    public sealed class HelloPayload
    {
        public string UserAgent { get; init; }

        public HelloPayload(string? userAgent = null) => UserAgent = userAgent ?? $"OoLunar.Willow/{typeof(ServerListener).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev"}";
    }
}
