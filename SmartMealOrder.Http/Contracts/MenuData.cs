using SmartMealOrder.Core.Models;

namespace SmartMealOrder.Http.Contracts;

internal sealed class MenuData
{
    public List<MenuItem> MenuItems { get; set; } = [];
}
