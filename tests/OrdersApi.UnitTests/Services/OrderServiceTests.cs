using FluentAssertions;
using OrdersApi.Application.Common;
using OrdersApi.Application.Orders;
using OrdersApi.Application.Orders.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Domain.Enums;
using OrdersApi.UnitTests.Helpers;

namespace OrdersApi.UnitTests.Services;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateOrder_WhenValidRequest()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.AddRange(
            OrdersHelper.GetActiveProduct1(),
            OrdersHelper.GetActiveProduct2());
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);

        var createOrderRequest = new CreateOrderRequest
        {
            CustomerId = OrdersHelper.DefaultCustomerId,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = OrdersHelper.DefaultProductId1, Quantity = 2 }, // 2x10 = 20
                new CreateOrderLineRequest { ProductId = OrdersHelper.DefaultProductId2, Quantity = 1 }
            ]
        };

        //Act
        var result = await orderService.CreateAsync(createOrderRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().BePositive();
        result.Value.CustomerId.Should().Be(OrdersHelper.DefaultCustomerId);
        result.Value.Status.Should().Be(nameof(OrderStatus.Pending));
        result.Value.Total.Should().Be(40.00m); // 20 + 20
        result.Value.Lines.Should().HaveCount(2);
        result.Value.Lines.ElementAt(1).Quantity.Should().Be(1);
        result.Value.Lines.Should()
            .ContainSingle(l => l.ProductId == OrdersHelper.DefaultProductId1 && l.Quantity == 2);
        result.Value.Lines.Should()
            .ContainSingle(l => l.ProductId == OrdersHelper.DefaultProductId2 && l.Quantity == 1);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenCustomerNotFound()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Products.Add(OrdersHelper.GetActiveProduct1());
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var createOrderRequest = new CreateOrderRequest
        {
            CustomerId = OrdersHelper.NonExistCustomerId,
            Lines =
            [
                new CreateOrderLineRequest
                {
                    ProductId = OrdersHelper.DefaultProductId1,
                    Quantity = 1
                }
            ]
        };

        //Act
        var result = await orderService.CreateAsync(createOrderRequest, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Contain("Customer");
        result.Error.Should().Contain(OrdersHelper.NonExistCustomerId.ToString());
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenProductIsInactive()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.Add(OrdersHelper.GetInactiveProduct());

        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var orderRequest = new CreateOrderRequest
        {
            CustomerId = OrdersHelper.DefaultCustomerId,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = OrdersHelper.InactiveProductId, Quantity = 2 }
            ]
        };

        //Act
        var result = await orderService.CreateAsync(orderRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Contain("not active");
        result.Error.Should().Contain(OrdersHelper.InactiveProductId.ToString());
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldUpdateToPaid_WhenStatusIsPending()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.Add(OrdersHelper.GetActiveProduct1());
        context.Orders.Add(new Order
        {
            Id = OrdersHelper.DefaultOrderId,
            CustomerId = OrdersHelper.DefaultCustomerId,
            Status = OrderStatus.Pending,
            Total = 10m,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var updateRequest = new UpdateOrderStatusRequest
        {
            Status = OrderStatus.Paid
        };

        //Act
        var result =
            await orderService.UpdateStatusAsync(OrdersHelper.DefaultOrderId, updateRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        var updatedOrder = await context.Orders.FindAsync(OrdersHelper.DefaultOrderId);
        updatedOrder!.Status.Should().Be(OrderStatus.Paid);
    }


    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnValidationError_WhenInvalidTransition()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.Add(OrdersHelper.GetActiveProduct1());
        context.Orders.Add(new Order
        {
            Id = OrdersHelper.DefaultOrderId,
            CustomerId = OrdersHelper.DefaultCustomerId,
            Status = OrderStatus.Paid,
            Total = 10m,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        });
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var updateRequest = new UpdateOrderStatusRequest
        {
            Status = OrderStatus.Pending
        };

        //Act
        var result =
            await orderService.UpdateStatusAsync(OrdersHelper.DefaultOrderId, updateRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BusinessRule);
        result.Error.Should().Contain("Pending");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenQuantityIsZero()
    {
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.Add(OrdersHelper.GetActiveProduct1());
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var updateRequest = new CreateOrderRequest
        {
            CustomerId = OrdersHelper.DefaultCustomerId,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = OrdersHelper.DefaultProductId1, Quantity = 0 }
            ]
        };

        //Act
        var result = await orderService.CreateAsync(updateRequest, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Contain("Quantity");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationError_WhenQuantityIsNegative()
    {
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.Add(OrdersHelper.GetActiveProduct1());
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var updateRequest = new CreateOrderRequest
        {
            CustomerId = OrdersHelper.DefaultCustomerId,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = OrdersHelper.DefaultProductId1, Quantity = -10 }
            ]
        };

        //Act
        var result = await orderService.CreateAsync(updateRequest, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Contain("Quantity");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnNotFoundError_WhenProductNotFound()
    {
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Customers.Add(OrdersHelper.GetCustomer());
        context.Products.Add(OrdersHelper.GetActiveProduct1());
        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var updateRequest = new CreateOrderRequest
        {
            CustomerId = OrdersHelper.DefaultCustomerId,
            Lines =
            [
                new CreateOrderLineRequest { ProductId = OrdersHelper.NonExistProductId, Quantity = 10 }
            ]
        };

        //Act
        var result = await orderService.CreateAsync(updateRequest, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("Product");
        result.Error.Should().Contain(OrdersHelper.NonExistProductId.ToString());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenOrderNotExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var orderService = new OrderService(context, OrdersHelper.CreateValidator);


        //Act
        var result = await orderService.GetByIdAsync(OrdersHelper.NonExistOrderId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain(OrdersHelper.NonExistOrderId.ToString());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOrder_WhenOrderExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Orders.Add(new Order
        {
            Id = OrdersHelper.DefaultOrderId,
            CustomerId = OrdersHelper.DefaultCustomerId,
            Status = OrderStatus.Pending,
            Total = 10m,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        });

        await context.SaveChangesAsync();

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);


        //Act
        var result = await orderService.GetByIdAsync(OrdersHelper.DefaultOrderId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(OrdersHelper.DefaultOrderId);
        result.Value.CustomerId.Should().Be(OrdersHelper.DefaultCustomerId);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedOrders_WhenOrderExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var filters = new OrderFilters
        {
            Page = 1,
            PageSize = 5
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(10);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByStatus_WhenStatusProvided()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var filters = new OrderFilters
        {
            Page = 1,
            PageSize = 5,
            Status = OrderStatus.Paid
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.Should().OnlyContain(o => o.Status == nameof(OrderStatus.Paid));
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByCustomerId_WhenCustomerIdProvided()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var filters = new OrderFilters
        {
            Page = 1,
            PageSize = 10,
            CustomerId = OrdersHelper.DefaultCustomerId
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(o => o.CustomerId == OrdersHelper.DefaultCustomerId);
        result.Value.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateFrom_WhenDateFromProvided()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var dateFrom = DateTime.UtcNow.AddDays(-3);

        var filters = new OrderFilters
        {
            Page = 1,
            PageSize = 20,
            DateFrom = dateFrom
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(o => o.CreatedAt >= dateFrom);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByDateTo_WhenDateToProvided()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var dateTo = DateTime.UtcNow.AddDays(-3);
        var filters = new OrderFilters
        {
            Page = 1,
            PageSize = 10,
            DateTo = dateTo
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(o => o.CreatedAt <= dateTo);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCombineMultipleFilters_WhenProvided()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var filters = new OrderFilters
        {
            Page = 1,
            PageSize = 10,
            Status = OrderStatus.Pending,
            CustomerId = OrdersHelper.DefaultCustomerId
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().OnlyContain(o =>
            o.CustomerId == OrdersHelper.DefaultCustomerId && o.Status == nameof(OrderStatus.Pending));
    }
    
    [Fact]
    public async Task GetAllAsync_ShouldReturnSecondPage_WhenPage2Requested()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await OrdersHelper.SeedTestData(context);

        var orderService = new OrderService(context, OrdersHelper.CreateValidator);
        var filters = new OrderFilters
        {
            Page = 2,
            PageSize = 5,
        };

        //Act
        var result = await orderService.GetAllAsync(filters, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(5);
        result.Value.Page.Should().Be(2);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }
}