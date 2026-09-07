namespace SmartMealOrder.Core.Models;

public sealed class OperationResult
{
    public bool Success { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public static OperationResult CreateSuccess()
    {
        return new OperationResult
        {
            Success = true
        };
    }

    public static OperationResult CreateError(string errorMessage)
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
