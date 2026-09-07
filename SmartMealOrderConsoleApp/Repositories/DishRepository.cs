using Microsoft.EntityFrameworkCore;
using SmartMealOrder.Core.Models;
using SmartMealOrderConsoleApp.Data;
using SmartMealOrderConsoleApp.Entities;

namespace SmartMealOrderConsoleApp.Repositories;

public class DishRepository : IDisposable
{
    private readonly AppDbContext _context;

    public DishRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task InitializeDatabaseAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task SaveDishesAsync(IReadOnlyCollection<MenuItem> menuItems)
    {
        ArgumentNullException.ThrowIfNull(menuItems);

        var existingDishes = await _context.Dishes.ToDictionaryAsync(x => x.ExternalId);
        var incomingExternalIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in menuItems)
        {
            incomingExternalIds.Add(item.Id);

            var code = string.IsNullOrWhiteSpace(item.Article)
                ? item.Id
                : item.Article;

            if (!existingDishes.TryGetValue(item.Id, out var dish))
            {
                dish = new DishEntity
                {
                    ExternalId = item.Id
                };

                _context.Dishes.Add(dish);
                existingDishes[item.Id] = dish;
            }

            dish.ExternalId = item.Id;
            dish.Code = code;
            dish.Name = item.Name;
            dish.Price = item.Price;
            dish.IsWeighted = item.IsWeighted;
        }

        var staleDishes = await _context.Dishes
            .Where(x => !incomingExternalIds.Contains(x.ExternalId))
            .ToListAsync();

        if (staleDishes.Count > 0)
        {
            _context.Dishes.RemoveRange(staleDishes);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<DishEntity?> GetDishByCodeAsync(string code)
    {
        return await _context.Dishes
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code);
    }

    public async Task<bool> DishExistsAsync(string code)
    {
        return await _context.Dishes
            .AsNoTracking()
            .AnyAsync(d => d.Code == code);
    }

    public async Task<List<DishEntity>> GetAllDishesAsync()
    {
        return await _context.Dishes
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}