// Models/Utils/Result.cs

using Skillbridge.Models;

namespace Skillbridge.Utilities;

/// <summary>
/// Resultado dos serviços
/// </summary>
public class Result
{
    public bool Success { get; }
    public string Message { get; }
    public string Additional { get; }
    public ErrorType ErrorType { get; }

    private Result(bool success, string message, string? additional, ErrorType errorType = ErrorType.Misc)
    {
        Success = success;
        Message = message;
        Additional = additional;
        ErrorType = errorType;
    }




    public static Result Ok(string message = "", string? additional = null) 
        => new(true, message, additional);

    public static Result Fail(string message, ErrorType errorType = ErrorType.Misc, string? additional = null) 
        => new(false, message, additional, errorType);


}