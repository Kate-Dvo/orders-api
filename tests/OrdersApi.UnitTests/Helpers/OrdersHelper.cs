using OrdersApi.Domain.Entities;

namespace OrdersApi.UnitTests.Helpers;

public static class OrdersHelper
{
    public const int DefaultOrderId = 1;
    public const int DefaultCustomerId = 1;
    public const int DefaultProductId1 = 1;
    public const int DefaultProductId2 = 2;
    public const int InactiveProductId = 99;
    public const int NonExistProductId = 999;
    public const int NonExistOrderId = 999;
    public const int NonExistCustomerId = 999;
    
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
}
