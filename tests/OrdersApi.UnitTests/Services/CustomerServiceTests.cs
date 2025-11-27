using FluentAssertions;
using OrdersApi.Application.Common;
using OrdersApi.Application.Customers;
using OrdersApi.Application.Customers.Models;
using OrdersApi.UnitTests.Helpers;

namespace OrdersApi.UnitTests.Services;

public class CustomerServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturn1Customers_When1CustomerExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer(), CustomersHelper.GetCustomer2());

        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var paginationParams = new PaginationParams
        {
            PageSize = 2,
            Page = 1
        };

        //Act
        var result = await customerService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Items.Should().NotBeNullOrEmpty();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Should().ContainSingle(c => c.Email == CustomersHelper.DefaultEmail);
        result.Value.Items.Should().ContainSingle(c => c.Email == CustomersHelper.DefaultEmail2);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturn1Customers_When2Exist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer(), CustomersHelper.GetCustomer2());

        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var paginationParams = new PaginationParams
        {
            PageSize = 1,
            Page = 1
        };

        //Act
        var result = await customerService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Items.Should().NotBeNullOrEmpty();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items.Should().ContainSingle(c => c.Email == CustomersHelper.DefaultEmail);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnNotFound_WhenCustomersNotExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var paginationParams = new PaginationParams
        {
            PageSize = 1,
            Page = 1
        };

        //Act
        var result = await customerService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Count.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenCustomerNotExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        //Act
        var result = await customerService.GetByIdAsync(CustomersHelper.DefaultId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldSuccess_WhenCustomerExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        await context.Customers.AddAsync(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        //Act
        var result = await customerService.GetByIdAsync(CustomersHelper.DefaultId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(CustomersHelper.DefaultId);
        result.Value.Name.Should().Be(CustomersHelper.DefaultName);
        result.Value.Email.Should().Be(CustomersHelper.DefaultEmail);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenCustomerEmailExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var createCustomerRequestBody = new CreateCustomerRequest
        {
            Name = CustomersHelper.DefaultName2,
            Email = CustomersHelper.DefaultEmail
        };

        //Act
        var result = await customerService.CreateAsync(createCustomerRequestBody, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain(CustomersHelper.DefaultEmail);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnResult_WhenSuccess()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.Add(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var createCustomerRequestBody = new CreateCustomerRequest
        {
            Name = CustomersHelper.DefaultName2,
            Email = CustomersHelper.DefaultEmail2
        };

        //Act
        var result = await customerService.CreateAsync(createCustomerRequestBody, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(CustomersHelper.DefaultEmail2);
        result.Value.Name.Should().Be(CustomersHelper.DefaultName2);
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.Id.Should().BePositive();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnConflict_WhenCustomerEmailExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer(), CustomersHelper.GetCustomer2());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var updateCustomerRequest = new UpdateCustomerRequest
        {
            Name = CustomersHelper.DefaultName,
            Email = CustomersHelper.DefaultEmail2
        };

        //Act
        var result =
            await customerService.UpdateAsync(CustomersHelper.DefaultId, updateCustomerRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("already exists");
        result.Error.Should().Contain(CustomersHelper.DefaultEmail2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenCustomerIdNotExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var updateCustomerRequest = new UpdateCustomerRequest
        {
            Name = CustomersHelper.DefaultName2,
            Email = CustomersHelper.DefaultEmail2
        };

        //Act
        var result = await customerService.UpdateAsync(CustomersHelper.DefaultId2, updateCustomerRequest,
            CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain(CustomersHelper.DefaultId2.ToString());
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnSuccess_WhenCustomerUpdated()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        var updateCustomerRequest = new UpdateCustomerRequest
        {
            Name = CustomersHelper.DefaultName2,
            Email = CustomersHelper.DefaultEmail
        };

        //Act
        var result =
            await customerService.UpdateAsync(CustomersHelper.DefaultId, updateCustomerRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccess_WhenCustomerDeleted()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        //Act
        var result = await customerService.DeleteAsync(CustomersHelper.DefaultId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenCustomerIdNotExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Customers.AddRange(CustomersHelper.GetCustomer());
        await context.SaveChangesAsync();

        var customerService =
            new CustomerService(context, CustomersHelper.CreateValidator, CustomersHelper.UpdateValidator);

        //Act
        var result = await customerService.DeleteAsync(CustomersHelper.DefaultId2, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain(CustomersHelper.DefaultId2.ToString());
    }
}