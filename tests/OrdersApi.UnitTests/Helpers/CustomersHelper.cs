using OrdersApi.Domain.Entities;

namespace OrdersApi.UnitTests.Helpers;

public static class CustomersHelper
{
    public const int DefaultId = 1;
    public const string DefaultName = "Customer Name";
    public const string DefaultEmail = "some@email.com";
    public const int DefaultId2 = 2;
    public const string DefaultName2 = "Customer Name2";
    public const string DefaultEmail2 = "some2@email.com";

    public static Customer GetCustomer()
    {
        return new Customer { Id = DefaultId, Name = DefaultName, Email = DefaultEmail, CreatedAt = DateTime.UtcNow };
    }

    public static Customer GetCustomer2()
    {
        return new Customer
            { Id = DefaultId2, Name = DefaultName2, Email = DefaultEmail2, CreatedAt = DateTime.UtcNow };
    }
}