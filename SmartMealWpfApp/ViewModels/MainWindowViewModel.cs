using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using SmartMealWpfApp.Configuration;
using SmartMealWpfApp.Storage;

namespace SmartMealWpfApp.ViewModels;

public sealed class MainWindowViewModel : ReactiveObject
{
    private readonly IEnvironmentVariablesStore _environmentVariablesStore;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel(
        IEnvironmentVariablesStore environmentVariablesStore,
        ILogger<MainWindowViewModel> logger,
        IOptions<EnvironmentVariablesOptions> options)
    {
        _environmentVariablesStore = environmentVariablesStore;
        _logger = logger;

        var variableNames = options.Value.Names
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Variables = new ObservableCollection<EnvironmentVariableItemViewModel>(
            _environmentVariablesStore
                .LoadOrInitialize(variableNames)
                .Select(static entry => new EnvironmentVariableItemViewModel(entry)));
    }

    public ObservableCollection<EnvironmentVariableItemViewModel> Variables { get; }

    public void Save(EnvironmentVariableItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.HasChanges)
        {
            return;
        }

        _environmentVariablesStore.Save(item.ToEntry());
        item.AcceptChanges();
        _logger.LogInformation("Environment variable row '{VariableName}' saved.", item.Name);
    }
}
