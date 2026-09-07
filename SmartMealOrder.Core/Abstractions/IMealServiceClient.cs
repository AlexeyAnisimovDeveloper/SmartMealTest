using SmartMealOrder.Core.Models;

namespace SmartMealOrder.Core.Abstractions;

public interface IMealServiceClient
{
    Task<MenuResponse> GetMenuAsync(bool withPrice, CancellationToken cancellationToken = default);

    Task<OperationResult> SendOrderAsync(Order order, CancellationToken cancellationToken = default);
}
