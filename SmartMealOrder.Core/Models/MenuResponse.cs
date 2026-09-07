namespace SmartMealOrder.Core.Models;

public sealed class MenuResponse
{
    public bool Success { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public IReadOnlyList<MenuItem> MenuItems { get; init; } = [];

    public static MenuResponse FromSuccess(IReadOnlyList<MenuItem> menuItems)
    {
        return new MenuResponse
        {
            Success = true,
            MenuItems = menuItems
        };
    }

    public static MenuResponse FromError(string errorMessage)
    {
        return new MenuResponse
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
