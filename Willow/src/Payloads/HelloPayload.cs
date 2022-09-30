using System.Reflection;

namespace OoLunar.Willow.Payloads
{
    public sealed class HelloPayload
    {
        public string UserAgent { get; init; }

        public HelloPayload(string? userAgent = null) => UserAgent = userAgent ?? $"OoLunar.Willow/{typeof(HelloPayload).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "dev"}";
    }
}
