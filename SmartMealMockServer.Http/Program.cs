using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Тестовые данные меню
var menuData = new
{
    Command = "GetMenu",
    Success = true,
    ErrorMessage = "",
    Data = new
    {
        MenuItems = new[]
        {
            new
            {
                Id = "5979224",
                Article = "A1004292",
                Name = "Каша гречневая",
                Price = 50m,
                IsWeighted = false,
                FullPath = "ПРОИЗВОДСТВО\\Гарниры",
                Barcodes = new[] { "57890975627974236429" }
            },
            new
            {
                Id = "9084246",
                Article = "A1004293",
                Name = "Конфеты Коровка",
                Price = 300m,
                IsWeighted = true,
                FullPath = "ДЕСЕРТЫ\\Развес",
                Barcodes = Array.Empty<string>()
            }
        }
    }
};


app.MapPost("/Meal", async (HttpContext context) =>
{
    return await ProcessOrderAsync(context);
});


async Task<IResult> ProcessOrderAsync(HttpContext context)
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();

    try
    {
        var request = JsonSerializer.Deserialize<JsonElement>(body);

        if (!request.TryGetProperty("Command", out var commandElement))
        {
            return Results.Json(new
            {
                Command = "Unknown",
                Success = false,
                ErrorMessage = "Command is required"
            });
        }

        var command = commandElement.GetString();

        if (command == "GetMenu")
        {
            var withPrice = false;

            if (request.TryGetProperty("CommandParameters", out var parameters) &&
                parameters.ValueKind == JsonValueKind.Object &&
                parameters.TryGetProperty("WithPrice", out var withPriceElement))
            {
                withPrice = withPriceElement.GetBoolean();
            }

            return Results.Json(withPrice ? menuData : new
            {
                Command = "GetMenu",
                Success = true,
                ErrorMessage = "",
                Data = new
                {
                    MenuItems = menuData.Data.MenuItems.Select(item => new
                    {
                        item.Id,
                        item.Article,
                        item.Name,
                        Price = 0m,
                        item.IsWeighted,
                        item.FullPath,
                        item.Barcodes
                    })
                }
            });
        }

        if (command == "SendOrder")
        {
            if (!request.TryGetProperty("CommandParameters", out var parameters) ||
                parameters.ValueKind != JsonValueKind.Object)
            {
                return Results.Json(new
                {
                    Command = "SendOrder",
                    Success = false,
                    ErrorMessage = "CommandParameters is required"
                });
            }

            if (!parameters.TryGetProperty("OrderId", out var orderIdElement) ||
                string.IsNullOrWhiteSpace(orderIdElement.GetString()))
            {
                return Results.Json(new
                {
                    Command = "SendOrder",
                    Success = false,
                    ErrorMessage = "OrderId is required"
                });
            }

            if (!parameters.TryGetProperty("MenuItems", out var menuItemsElement) ||
                menuItemsElement.ValueKind != JsonValueKind.Array ||
                menuItemsElement.GetArrayLength() == 0)
            {
                return Results.Json(new
                {
                    Command = "SendOrder",
                    Success = false,
                    ErrorMessage = "MenuItems must contain at least one item"
                });
            }

            return Results.Json(new
            {
                Command = "SendOrder",
                Success = true,
                ErrorMessage = ""
            });
        }

        return Results.Json(new
        {
            Command = command ?? "Unknown",
            Success = false,
            ErrorMessage = "Unsupported command"
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            Command = "Unknown",
            Success = false,
            ErrorMessage = $"Invalid JSON: {ex.Message}"
        });
    }
}


app.MapGet("/", () => "  HTTP Mock Server is running on port 6001\n" +
    "  Endpoint: POST /Meal (для всех команд)\n"
    );

Console.WriteLine("  Starting HTTP Mock Server...");

app.Run("http://localhost:6001");
