namespace SmartMealOrder.Http.Options;

public sealed class HttpMealClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
