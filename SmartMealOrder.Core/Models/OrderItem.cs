namespace SmartMealOrder.Core.Models;

public sealed class OrderItem
{
    public string Id { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
}
