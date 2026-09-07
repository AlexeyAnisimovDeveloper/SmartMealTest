using SmartMealWpfApp.Models;

namespace SmartMealWpfApp.Storage;

public interface IEnvironmentVariablesStore
{
    IReadOnlyList<EnvironmentVariableEntry> LoadOrInitialize(IReadOnlyCollection<string> variableNames);

    void Save(EnvironmentVariableEntry entry);
}
