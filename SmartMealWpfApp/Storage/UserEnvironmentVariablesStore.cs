using System.Text.Json;
using Microsoft.Extensions.Logging;
using SmartMealWpfApp.Models;

namespace SmartMealWpfApp.Storage;

public sealed class UserEnvironmentVariablesStore(ILogger<UserEnvironmentVariablesStore> logger) : IEnvironmentVariablesStore
{
    private const string DefaultValue = "Default value";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public IReadOnlyList<EnvironmentVariableEntry> LoadOrInitialize(IReadOnlyCollection<string> variableNames)
    {
        var entries = new List<EnvironmentVariableEntry>(variableNames.Count);

        foreach (var variableName in variableNames.Where(static name => !string.IsNullOrWhiteSpace(name)))
        {
            var currentValue = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);

            if (string.IsNullOrWhiteSpace(currentValue))
            {
                var defaultEntry = new EnvironmentVariableEntry
                {
                    Name = variableName,
                    Value = DefaultValue,
                    Comment = string.Empty
                };

                Save(defaultEntry);
                entries.Add(defaultEntry);
                continue;
            }

            entries.Add(Parse(variableName, currentValue));
        }

        return entries;
    }

    public void Save(EnvironmentVariableEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var serializedValue = JsonSerializer.Serialize(new EnvironmentVariablePayload
        {
            Value = entry.Value ?? string.Empty,
            Comment = entry.Comment ?? string.Empty
        }, SerializerOptions);

        Environment.SetEnvironmentVariable(entry.Name, serializedValue, EnvironmentVariableTarget.User);
        logger.LogInformation("Environment variable '{VariableName}' persisted for user scope. Value: \"{Value}\". Comment: \"{Comment}\".",
            entry.Name,
            entry.Value ?? string.Empty,
            entry.Comment ?? string.Empty);
    }

    private static EnvironmentVariableEntry Parse(string variableName, string rawValue)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<EnvironmentVariablePayload>(rawValue, SerializerOptions);

            if (payload is not null)
            {
                return new EnvironmentVariableEntry
                {
                    Name = variableName,
                    Value = payload.Value ?? string.Empty,
                    Comment = payload.Comment ?? string.Empty
                };
            }
        }
        catch (JsonException)
        {
        }

        return new EnvironmentVariableEntry
        {
            Name = variableName,
            Value = rawValue,
            Comment = string.Empty
        };
    }
}
