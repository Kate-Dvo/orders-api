using Microsoft.EntityFrameworkCore;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.UnitTests.Helpers;

public class TestDbContextFactory
{
    public static OrdersDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new OrdersDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }
}