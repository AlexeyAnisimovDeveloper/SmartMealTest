namespace SmartMealWpfApp.Models;

public sealed class EnvironmentVariableEntry
{
    public required string Name { get; init; }

    public string Value { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;
}
