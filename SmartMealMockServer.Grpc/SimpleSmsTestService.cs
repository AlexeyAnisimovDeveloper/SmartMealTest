using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Sms.Test;

public class SimpleSmsTestService : SmsTestService.SmsTestServiceBase
{
    // Тестовые данные меню
    private static readonly MenuItem[] MenuItems =
    {
        new()
        {
            Id = "5979224",
            Article = "A1004292",
            Name = "Каша гречневая",
            Price = 50,
            IsWeighted = false,
            FullPath = "ПРОИЗВОДСТВО\\Гарниры"
        },
        new()
        {
            Id = "9084246",
            Article = "A1004293",
            Name = "Конфеты Коровка",
            Price = 300,
            IsWeighted = true,
            FullPath = "ДЕСЕРТЫ\\Развес"
        }
    };

    public override Task<GetMenuResponse> GetMenu(BoolValue request, ServerCallContext context)
    {
        var withPrice = request?.Value ?? false;

        var response = new GetMenuResponse { Success = true };

        foreach (var item in MenuItems)
        {
            response.MenuItems.Add(new MenuItem
            {
                Id = item.Id,
                Article = item.Article,
                Name = item.Name,
                Price = withPrice ? item.Price : 0,
                IsWeighted = item.IsWeighted,
                FullPath = item.FullPath
            });
        }

        return Task.FromResult(response);
    }

    public override Task<SendOrderResponse> SendOrder(Order request, ServerCallContext context)
    {
        // Базовая валидация
        if (string.IsNullOrEmpty(request.Id))
        {
            return Task.FromResult(new SendOrderResponse
            {
                Success = false,
                ErrorMessage = "OrderId is required"
            });
        }

        if (request.OrderItems.Count == 0)
        {
            return Task.FromResult(new SendOrderResponse
            {
                Success = false,
                ErrorMessage = "OrderItems must contain at least one item"
            });
        }

        return Task.FromResult(new SendOrderResponse { Success = true });
    }
}