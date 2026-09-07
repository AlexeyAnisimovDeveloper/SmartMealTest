using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SmartMealOrder.Core.Abstractions;
using SmartMealOrder.Grpc.Options;
using SmartMealOrder.Grpc.Services;
using SmartMealOrder.Http.Options;
using SmartMealOrder.Http.Services;
using SmartMealOrder.Core.Models;
using SmartMealOrderConsoleApp.Data;
using SmartMealOrderConsoleApp.Repositories;

namespace SmartMealOrderConsoleApp;

internal class Program
{
    private static IConfiguration _configuration = null!;
    private static ILoggerFactory _loggerFactory = null!;
    private static ILogger<Program> _logger = null!;
    private static DishRepository _dishRepository = null!;
    private static IMealServiceClient _mealService = null!;

    private static async Task Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        try
        {
            InitializeConfiguration();
            InitializeLogger();
            InitializeInfrastructure();

            _logger.LogInformation("=== APPLICATION START ===");
            Console.WriteLine("=== APPLICATION START ===");
            Console.WriteLine();

            await InitializeDatabaseAsync();
            var menuLoaded = await LoadAndShowMenuAsync();

            if (!menuLoaded)
            {
                return;
            }

            // ==> ЦИКЛ: Повторяем заказы пока пользователь не нажмет ESC
            while (true)
            {
                var order = await BuildOrderAsync();
                await SendOrderAsync(order);

                Console.WriteLine();
                Console.WriteLine("Нажмите ESC для выхода или любую другую клавишу для продолжения...");
                var key = Console.ReadKey(intercept: true);
                Console.WriteLine();

                if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            _logger.LogInformation("=== APPLICATION COMPLETED SUCCESSFULLY ===");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Critical application error");
            Console.WriteLine();
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            _dishRepository?.Dispose();
            if (_mealService is IDisposable disposableMealService)
            {
                disposableMealService.Dispose();
            }

            _loggerFactory?.Dispose();
            Log.CloseAndFlush();

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }

    private static void InitializeConfiguration()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();
    }

    private static void InitializeLogger()
    {
        var logFilePath = Path.Combine(
            AppContext.BaseDirectory,
            $"test-sms-console-app-{DateTime.Now:yyyyMMdd}.log");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(_configuration)
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Infinite,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
        _logger = _loggerFactory.CreateLogger<Program>();
        _logger.LogInformation("Logging initialized. File: {LogFilePath}", logFilePath);
    }

    private static void InitializeInfrastructure()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }

        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context = new AppDbContext(dbOptions);

        _dishRepository = new DishRepository(context);
        _mealService = CreateMealServiceClient();
    }

    private static IMealServiceClient CreateMealServiceClient()
    {
        var transport = _configuration["ClientSettings:Transport"] ?? "Http";

        if (string.Equals(transport, "Grpc", StringComparison.OrdinalIgnoreCase))
        {
            var options = new GrpcMealClientOptions
            {
                Address = _configuration["GrpcClient:Address"] ?? "http://localhost:5123"
            };

            _logger.LogInformation("Using gRPC client. Address: {Address}", options.Address);
            return new GrpcMealServiceClient(options);
        }

        var httpOptions = new HttpMealClientOptions
        {
            BaseUrl = _configuration["HttpClient:BaseUrl"] ?? "http://localhost:6001/Meal",
            Username = _configuration["HttpClient:Username"] ?? "admin",
            Password = _configuration["HttpClient:Password"] ?? "admin123"
        };

        _logger.LogInformation("Using HTTP client. BaseUrl: {BaseUrl}", httpOptions.BaseUrl);
        return new HttpMealServiceClient(new HttpClient(), httpOptions);
    }

    private static async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Initializing database...");
        Console.WriteLine("Инициализация базы данных...");

        await _dishRepository.InitializeDatabaseAsync();

        _logger.LogInformation("Database initialized successfully.");
        Console.WriteLine("База данных готова.");
        Console.WriteLine();
    }

    private static async Task<bool> LoadAndShowMenuAsync()
    {
        _logger.LogInformation("Retrieving menu from configured service...");
        Console.WriteLine("Получение меню...");

        var menuResponse = await _mealService.GetMenuAsync(withPrice: true);

        if (!menuResponse.Success)
        {
            Console.WriteLine();
            Console.WriteLine($"Ошибка: {menuResponse.ErrorMessage}");
            _logger.LogWarning("Menu loading failed: {ErrorMessage}", menuResponse.ErrorMessage);
            return false;
        }

        if (menuResponse.MenuItems.Count == 0)
        {
            throw new InvalidOperationException("Сервис вернул пустое меню.");
        }

        await _dishRepository.SaveDishesAsync(menuResponse.MenuItems);

        var dishes = await _dishRepository.GetAllDishesAsync();

        Console.WriteLine();
        Console.WriteLine("=== СПИСОК БЛЮД ===");
        Console.WriteLine("Наименование | Код (артикул) | Цена за единицу");
        Console.WriteLine(new string('-', 70));

        foreach (var dish in dishes)
        {
            Console.WriteLine($"{dish.Name,-30} | {dish.Code,-15} | {dish.Price,10:F2} RUB");
        }

        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"Всего блюд: {dishes.Count}");
        Console.WriteLine();

        _logger.LogInformation("Menu successfully loaded, saved to DB and displayed.");
        return true;
    }

    private static async Task<Order> BuildOrderAsync()
    {
        while (true)
        {
            Console.WriteLine("Введите позиции заказа в формате:");
            Console.WriteLine("Код1:Количество1;Код2:Количество2;Код3:Количество3;");
            Console.WriteLine("Пример: A1004292:1;A1004293:0.408;");
            Console.Write("> ");

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Ввод не может быть пустым.");
                Console.WriteLine();
                continue;
            }

            try
            {
                var parsedItems = ParseOrderInput(input);
                var orderItems = await BuildOrderItemsAsync(parsedItems);

                var order = new Order
                {
                    Id = Guid.NewGuid().ToString().ToUpperInvariant()
                };

                order.Items.AddRange(orderItems);

                _logger.LogInformation("Order created. OrderId: {OrderId}, Items: {ItemsCount}", order.Id, order.Items.Count);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid order input.");
                Console.WriteLine();
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine();
            }
        }
    }

    private static List<ParsedOrderItem> ParseOrderInput(string input)
    {
        var result = new List<ParsedOrderItem>();
        var parts = input.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var pair = part.Split(':', StringSplitOptions.TrimEntries);

            if (pair.Length != 2)
            {
                throw new InvalidOperationException($"Неверный формат элемента '{part}'. Ожидается Код:Количество.");
            }

            var code = pair[0];

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Код блюда не может быть пустым.");
            }

            var normalizedQuantity = pair[1].Replace(',', '.');

            if (!decimal.TryParse(
                    normalizedQuantity,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var quantity))
            {
                throw new InvalidOperationException($"Количество для кода '{code}' задано неверно.");
            }

            if (quantity <= 0m)
            {
                throw new InvalidOperationException($"Количество для кода '{code}' должно быть больше нуля.");
            }

            result.Add(new ParsedOrderItem(code, quantity));
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException("Не введено ни одной позиции заказа.");
        }

        return result;
    }

    private static async Task<List<OrderItem>> BuildOrderItemsAsync(List<ParsedOrderItem> parsedItems)
    {
        var result = new List<OrderItem>();
        var validationErrors = new List<string>();

        foreach (var parsedItem in parsedItems)
        {
            var dish = await _dishRepository.GetDishByCodeAsync(parsedItem.Code);

            if (dish is null)
            {
                validationErrors.Add($"Код '{parsedItem.Code}' не найден в загруженном меню.");
                continue;
            }

            if (!dish.IsWeighted && parsedItem.Quantity != decimal.Truncate(parsedItem.Quantity))
            {
                validationErrors.Add(
                    $"Блюдо '{dish.Name}' с кодом '{dish.Code}' не является весовым. Для него нужно целое количество.");
                continue;
            }

            result.Add(new OrderItem
            {
                Id = dish.ExternalId,
                Quantity = parsedItem.Quantity
            });
        }

        if (validationErrors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
        }

        return result;
    }

    private static async Task SendOrderAsync(Order order)
    {
        _logger.LogInformation("Sending order. OrderId: {OrderId}", order.Id);
        Console.WriteLine();
        Console.WriteLine("Отправка заказа...");

        var response = await _mealService.SendOrderAsync(order);

        Console.WriteLine();
        Console.WriteLine("=== ОТВЕТ СЕРВЕРА ===");

        if (response.Success)
        {
            Console.WriteLine("УСПЕХ");
            Console.WriteLine($"OrderId: {order.Id}");
            _logger.LogInformation("Order sent successfully. OrderId: {OrderId}", order.Id);
        }
        else
        {
            Console.WriteLine($"Ошибка: {response.ErrorMessage}");
            _logger.LogWarning("Order sending failed. OrderId: {OrderId}. Error: {Error}", order.Id, response.ErrorMessage);
        }

        Console.WriteLine(new string('=', 40));
    }

    private sealed record ParsedOrderItem(string Code, decimal Quantity);
}
