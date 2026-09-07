namespace SmartMealOrder.Core.Models;

public sealed class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public List<OrderItem> Items { get; } = [];
}
