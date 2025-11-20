using FluentAssertions;
using OrdersApi.Application.Common;
using OrdersApi.Application.Products;
using OrdersApi.Application.Products.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.UnitTests.Helpers;

namespace OrdersApi.UnitTests.Services;

public class ProductServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProducts_WhenProductsExist()
    {
        //Arrange - setup test data
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Products.AddRange(
            new Product { Id = 1, Sku = "SKU-001", Name = "Product 1", Price = 10.00m, IsActive = true },
            new Product { Id = 2, Sku = "SKU-002", Name = "Product 2", Price = 20.00m, IsActive = false },
            new Product { Id = 3, Sku = "SKU-003", Name = "Product 3", Price = 30.00m, IsActive = true }
        );

        await context.SaveChangesAsync();

        var productService = new ProductService(context);

        //Act
        var result = await productService.GetAllAsync(CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().HaveCount(3);
        result.Value.Should().ContainSingle(p => p.Sku == "SKU-001");
        result.Value.Should().ContainSingle(p => p.Sku == "SKU-002");
        result.Value.Should().ContainSingle(p => p.Sku == "SKU-003");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenSkuAlreadyExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Products.Add(GetProduct());
        await context.SaveChangesAsync();

        var productService = new ProductService(context);

        var newProductRequest = new CreateProductRequest
        {
            Sku = "SKU-001",
            Name = "New product",
            Price = 100.00m,
            IsActive = true
        };

        //Act
        var result = await productService.CreateAsync(newProductRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("SKU-001");
        result.Error.Should().Contain("already exist");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        var productService = new ProductService(context);

        //Act
        var result = await productService.GetByIdAsync(999, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain("999");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldSuccess_WhenProductExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        
        await context.Products.AddAsync(GetProduct());
        await context.SaveChangesAsync();
        
        var productId = 1;
        var productService = new ProductService(context);

        //Act
        var result = await productService.GetByIdAsync(productId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(productId);
        result.Value.Sku.Should().Be("SKU-001");
        result.Value.Name.Should().Be("Product name");
        result.Value.Price.Should().Be(50.00m);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProduct_WhenSkuIsUnique()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var productService = new ProductService(context);

        //Act
        var result = await productService.CreateAsync(
            new CreateProductRequest
            {
                Sku = "UNIQUE_SKU",
                Name = "Product 1",
                Price = 100.00m,
                IsActive = true
            }, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Sku.Should().Be("UNIQUE_SKU");
        result.Value.Name.Should().Be("Product 1");
        result.Value.Price.Should().Be(100.00m);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldSuccess_WhenUpdatingSameProductSku()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        context.Products.Add(GetProduct());
        var productId = 1;
        await context.SaveChangesAsync();

        var productService = new ProductService(context);
        var productUpdateRequest = new UpdateProductRequest
        {
            Sku = "SKU-001",
            Name = "New product",
            Price = 100.00m,
            IsActive = true
        };

        //Act
        var result = await productService.UpdateAsync(productId, productUpdateRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnConflict_WhenSkuExistInDifferentProduct()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Products.AddRange(
            GetProduct(),
            new Product
            {
                Id = 2,
                Sku = "SKU-002",
                Name = "Product2 name",
                Price = 50.00m,
                IsActive = true
            });

        await context.SaveChangesAsync();

        var productService = new ProductService(context);
        var updateRequest = new UpdateProductRequest
        {
            Sku = "SKU-002",
            Name = "New product",
            Price = 100.00m,
            IsActive = true
        };

        //Act
        var result = await productService.UpdateAsync(1, updateRequest, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Should().Contain("SKU-002");
        result.Error.Should().Contain("already exist");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSucceed_WhenProductExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Products.Add(GetProduct());
        await context.SaveChangesAsync();
        var productService = new ProductService(context);

        //Act
        var result = await productService.DeleteAsync(1, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldFail_WhenProductIdNotExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        var productService = new ProductService(context);
        var productId = 1;

        //Act
        var result = await productService.DeleteAsync(productId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain(productId.ToString());
    }

    private static Product GetProduct()
    {
        return new Product
        {
            Id = 1,
            Sku = "SKU-001",
            Name = "Product name",
            Price = 50.00m,
            IsActive = true
        };
    }
}