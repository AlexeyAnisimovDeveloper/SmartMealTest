using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SmartMealOrder.Core.Abstractions;
using SmartMealOrder.Http.Contracts;
using SmartMealOrder.Http.Options;
using SmartMealOrder.Core.Models;

namespace SmartMealOrder.Http.Services;

public sealed class HttpMealServiceClient : IMealServiceClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public HttpMealServiceClient(HttpClient httpClient, HttpMealClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            throw new ArgumentException("BaseUrl is required.", nameof(options));
        }

        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<MenuResponse> GetMenuAsync(bool withPrice, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            Command = "GetMenu",
            CommandParameters = new
            {
                WithPrice = withPrice
            }
        };

        var result = await PostAsync<ServerResponse<MenuData>>(request, cancellationToken);

        if (!result.Success)
        {
            return MenuResponse.FromError(result.ErrorMessage);
        }

        return MenuResponse.FromSuccess(result.Data?.MenuItems ?? []);
    }

    public async Task<OperationResult> SendOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var request = new
        {
            Command = "SendOrder",
            CommandParameters = new
            {
                OrderId = order.Id,
                MenuItems = order.Items.Select(item => new
                {
                    item.Id,
                    Quantity = item.Quantity.ToString(CultureInfo.InvariantCulture)
                })
            }
        };

        var result = await PostAsync<ServerResponse<object?>>(request, cancellationToken);

        return result.Success
            ? OperationResult.CreateSuccess()
            : OperationResult.CreateError(result.ErrorMessage);
    }

    private async Task<T> PostAsync<T>(object request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(string.Empty, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        try
        {
            return JsonSerializer.Deserialize<T>(responseJson, SerializerOptions)
                ?? throw new InvalidOperationException("Failed to deserialize the server response.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON parsing error: {ex.Message}", ex);
        }
    }
}
