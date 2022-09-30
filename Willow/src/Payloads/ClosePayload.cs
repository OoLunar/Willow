using System;

namespace OoLunar.Willow.Payloads
{
    public class ClosePayload
    {
        public CloseCode ErrorCode { get; init; }
        public string? Message { get; init; }

        public ClosePayload(CloseCode errorCode, string? message)
        {
            ErrorCode = errorCode;
            Message = message;
        }
    }

    [Flags]
    public enum CloseCode
    {
        ServerShutdown,
        InvalidHello,
        InvalidLogin
    }
}
