using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.UnitTests.Helpers;

public static class TestDbContextFactory
{
    public static OrdersDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        
        var context = new OrdersDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }
}