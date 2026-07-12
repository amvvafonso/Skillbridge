// Models/Utils/Result.cs

using Skillbridge.Models;

namespace Skillbridge.Utilities;

public class Result
{
    public bool Success { get; }
    public string Message { get; }
    
    public ErrorType ErrorType { get; }

    private Result(bool success, string message, ErrorType errorType = default)
    {
        Success = success;
        Message = message;
        ErrorType = errorType;
    }

    public static Result Ok(string message = "") => new(true, message);
    public static Result Fail(string message, ErrorType errorType) => new(false, message, errorType);
}