using System;

namespace OoLunar.Willow.Payloads
{
    public class ErrorPayload
    {
        public ErrorCode ErrorCode { get; init; }
        public string? Message { get; init; }
        public object? Data { get; init; }

        public ErrorPayload(ErrorCode errorCode, string? message, object? data = null)
        {
            ErrorCode = errorCode;
            Message = message;
            Data = data;
        }
    }

    [Flags]
    public enum ErrorCode
    {
        InvalidAction,
        Unauthorized,
        InvalidData,
        ServerError
    }
}
