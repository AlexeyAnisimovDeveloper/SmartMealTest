using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using SmartMealOrder.Core.Abstractions;
using SmartMealOrder.Grpc.Options;
using SmartMealOrder.Core.Models;
using CoreMenuItem = SmartMealOrder.Core.Models.MenuItem;
using CoreOrder = SmartMealOrder.Core.Models.Order;
using GrpcClient = Sms.Test.SmsTestService.SmsTestServiceClient;
using OrderRequest = Sms.Test.Order;
using OrderItemRequest = Sms.Test.OrderItem;

namespace SmartMealOrder.Grpc.Services;

public sealed class GrpcMealServiceClient : IMealServiceClient, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly GrpcClient _client;

    public GrpcMealServiceClient(GrpcMealClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Address))
        {
            throw new ArgumentException("Address is required.", nameof(options));
        }

        _channel = GrpcChannel.ForAddress(options.Address);
        _client = new GrpcClient(_channel);
    }

    public async Task<MenuResponse> GetMenuAsync(bool withPrice, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetMenuAsync(new BoolValue { Value = withPrice }, cancellationToken: cancellationToken);

        if (!response.Success)
        {
            return MenuResponse.FromError(response.ErrorMessage);
        }

        var menuItems = response.MenuItems
            .Select(item => new CoreMenuItem
            {
                Id = item.Id,
                Article = item.Article,
                Name = item.Name,
                Price = Convert.ToDecimal(item.Price),
                IsWeighted = item.IsWeighted,
                FullPath = item.FullPath,
                Barcodes = item.Barcodes.ToList()
            })
            .ToList();

        return MenuResponse.FromSuccess(menuItems);
    }

    public async Task<OperationResult> SendOrderAsync(CoreOrder order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var request = new OrderRequest
        {
            Id = order.Id
        };

        request.OrderItems.AddRange(order.Items.Select(item => new OrderItemRequest
        {
            Id = item.Id,
            Quantity = Convert.ToDouble(item.Quantity)
        }));

        var response = await _client.SendOrderAsync(request, cancellationToken: cancellationToken);

        return response.Success
            ? OperationResult.CreateSuccess()
            : OperationResult.CreateError(response.ErrorMessage);
    }

    public void Dispose()
    {
        _channel.Dispose();
    }
}
