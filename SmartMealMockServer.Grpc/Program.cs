var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

// Регистрация простого сервиса
app.MapGrpcService<SimpleSmsTestService>();

app.MapGet("/", () => "  gRPC Mock Server is running on port 5123");

Console.WriteLine("  Starting gRPC Mock Server...");

app.Run("http://localhost:5123");