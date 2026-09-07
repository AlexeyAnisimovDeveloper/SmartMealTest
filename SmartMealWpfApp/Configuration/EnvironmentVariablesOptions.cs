namespace SmartMealWpfApp.Configuration;

public sealed class EnvironmentVariablesOptions
{
    public const string SectionName = "EnvironmentVariables";

    public string[] Names { get; init; } = [];
}
