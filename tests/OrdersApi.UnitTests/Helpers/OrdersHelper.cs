using OrdersApi.Application.Orders.Validators;
using OrdersApi.Domain.Entities;
using OrdersApi.Domain.Enums;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.UnitTests.Helpers;

public static class OrdersHelper
{
    public const int DefaultOrderId = 1;
    public const int DefaultCustomerId = 1;
    public const int DefaultCustomerId2 = 2;
    public const int DefaultProductId1 = 1;
    public const int DefaultProductId2 = 2;
    public const int InactiveProductId = 99;
    public const int NonExistProductId = 999;
    public const int NonExistOrderId = 999;
    public const int NonExistCustomerId = 999;

    public static CreateOrderRequestValidator CreateValidator => new();

    public static Customer GetCustomer()
    {
        return new Customer
        {
            Id = DefaultCustomerId,
            Name = "Test Customer",
            Email = "customer@test.com",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Customer GetCustomer2()
    {
        return new Customer
        {
            Id = DefaultCustomerId2,
            Name = "Test Customer2",
            Email = "customer2@test.com",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Product GetActiveProduct1()
    {
        return new Product
        {
            Id = DefaultProductId1,
            Sku = "PROD-001",
            Name = "Active Product 1",
            Price = 10.00m,
            IsActive = true
        };
    }

    public static Product GetActiveProduct2()
    {
        return new Product
        {
            Id = DefaultProductId2,
            Sku = "PROD-002",
            Name = "Active Product 2",
            Price = 20.00m,
            IsActive = true
        };
    }

    public static Product GetInactiveProduct()
    {
        return new Product
        {
            Id = InactiveProductId,
            Sku = "PROD-INACTIVE",
            Name = "Inactive Product",
            Price = 50.00m,
            IsActive = false
        };
    }

    public static async Task SeedTestData(OrdersDbContext context)
    {
        context.Customers.AddRange(GetCustomer(), GetCustomer2());

        var orders = new List<Order>();

        for (var i = 1; i <= 10; i++)
        {
            var status = i switch
            {
                <= 5 => OrderStatus.Pending,
                <= 8 => OrderStatus.Paid,
                _ => OrderStatus.Cancelled
            };

            orders.Add(new Order
            {
                Id = i,
                CustomerId = i <= 5 ? DefaultCustomerId : DefaultCustomerId2,
                Status = status,
                Total = i * 10.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-i),
                RowVersion = new byte[8]
            });
        }

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();
    }
}