using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SmartMealWpfApp.Models;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SmartMealWpfApp.ViewModels;


/*
 * Использование ReactiveUI сделано для примера. Для для данного ViewModel это может быть избыточно,
 * но в реальном приложении может быть полезно для более сложных (или очень сложных) сценариев.
 * Например, когда нужно будет отслеживать изменения в нескольких свойствах и выполнять действия
 * или вычисления (за которыми можно наблюдать) на основе этих изменений.
 */

public sealed partial class EnvironmentVariableItemViewModel : ReactiveObject
{
    [Reactive] public partial string Value { get; set; }

    [Reactive] public partial string Comment { get; set; }

    [ObservableAsProperty] public partial bool HasChanges { get; }

    private string _originalValue = string.Empty;
    private string _originalComment = string.Empty;
    private readonly Subject<Unit> _hasChangesRefresh = new();

    public EnvironmentVariableItemViewModel(EnvironmentVariableEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Name = entry.Name;
        Value = entry.Value ?? string.Empty;
        Comment = entry.Comment ?? string.Empty;
        _originalValue = Value;
        _originalComment = Comment;

        Observable.Merge(
                this.WhenAnyValue(x => x.Value).Select(_ => Unit.Default),
                this.WhenAnyValue(x => x.Comment).Select(_ => Unit.Default),
                _hasChangesRefresh)
            .StartWith(Unit.Default)
            .Select(_ => !string.Equals(Value, _originalValue, StringComparison.Ordinal)
                || !string.Equals(Comment, _originalComment, StringComparison.Ordinal))
            .ToProperty(this, x => x.HasChanges, out _hasChangesHelper);
    }

    public string Name { get; }

    public EnvironmentVariableEntry ToEntry() => new()
    {
        Name = Name,
        Value = Value,
        Comment = Comment
    };

    public void AcceptChanges()
    {
        _originalValue = Value;
        _originalComment = Comment;
        _hasChangesRefresh.OnNext(Unit.Default);
    }
}
