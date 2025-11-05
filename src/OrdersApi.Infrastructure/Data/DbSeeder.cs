using Microsoft.Extensions.Logging;
using OrdersApi.Domain.Entities;

namespace OrdersApi.Infrastructure.Data;

public static class DbSeeder
{
    public static void SeedData(this OrdersDbContext context, ILogger logger)
    {
        try
        {
            if (context.Customers.Any() || context.Products.Any())
            {
                return;
            }

            //Seed Customers
            var customers = new[]
            {
                new Customer
                {
                    Name = "John Doe",
                    Email = "john.doe@example.com",
                    CreatedAt = DateTime.UtcNow,
                },
                new Customer
                {
                    Name = "Jane Smith",
                    Email = "jane.smith@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                },
                new Customer
                {
                    Name = "Israel Israeli",
                    Email = "israel.israely@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                }
            };

            context.Customers.AddRange(customers);
            context.SaveChanges();

            //Seed Products
            var products = new[]
            {
                new Product
                {
                    Sku = "LAPTOP-001",
                    Name = "High-Performance Laptop",
                    Price = 1299.99m,
                    IsActive = true
                },
                new Product
                {
                    Sku = "MOUSE-002",
                    Name = "Wireless Ergonomic Mouse",
                    Price = 49.99m,
                    IsActive = true
                },
                new Product
                {
                    Sku = "KEYBOARD-003",
                    Name = "Mechanical RGB Keyboard",
                    Price = 129.99m,
                    IsActive = true
                },
                new Product
                {
                    Sku = "MONITOR-004",
                    Name = "27-inch 4K Monitor",
                    Price = 399.99m,
                    IsActive = true
                },
                new Product
                {
                    Sku = "HEADSET-005",
                    Name = "Noise-Cancelling Headset",
                    Price = 89.99m,
                    IsActive = false
                }
            };

            context.Products.AddRange(products);
            context.SaveChanges();

            logger.LogInformation(
                "Database seeded successfully! {CustomersLength} customers added, {ProductsLength} products added.",
                customers.Length, products.Length);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error occured while seeding the database.");
            throw;
        }
    }
}