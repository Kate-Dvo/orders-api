using OrdersApi.Application.Products.Validators;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.UnitTests.Helpers;

public static class ProductHelper
{
    public static CreateProductRequestValidator CreateValidator => new();
    public static UpdateProductRequestValidator UpdateValidator => new();

    public static async Task SeedProducts(OrdersDbContext context, int count)
    {
        var products = Enumerable.Range(1, count)
            .Select(i => new Product
            {
                Id = i,
                Sku = $"SKU-{i:D3}",
                Name = $"Product {i:D3}",
                Price = i * 1.00m,
                IsActive = true
            }).ToList();
        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}