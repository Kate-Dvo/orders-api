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
        var paginationParams = new PaginationParams
        {
            Page = 1,
            PageSize = 3,
        };

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value?.Items.Should().NotBeNullOrEmpty();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Items.Should().ContainSingle(p => p.Sku == "SKU-001");
        result.Value.Items.Should().ContainSingle(p => p.Sku == "SKU-002");
        result.Value.Items.Should().ContainSingle(p => p.Sku == "SKU-003");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnConflict_WhenSkuAlreadyExists()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        context.Products.Add(GetProduct());
        await context.SaveChangesAsync();

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);

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

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);

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
        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);

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
        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);

        //Act
        var result = await productService.CreateAsync(
            new CreateProductRequest
            {
                Sku = "UNIQUE-SKU",
                Name = "Product 1",
                Price = 100.00m,
                IsActive = true
            }, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Sku.Should().Be("UNIQUE-SKU");
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

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
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

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
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
        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);

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
        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var productId = 1;

        //Act
        var result = await productService.DeleteAsync(productId, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Should().Contain("not found");
        result.Error.Should().Contain(productId.ToString());
    }

    //pagination tests
    [Fact]
    public async Task GetAllAsync_ShouldReturnCorrectPage_WhenRequestingFirstPage()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 10);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 3 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.PageSize.Should().Be(3);
        result.Value.TotalCount.Should().Be(10);
        result.Value.TotalPages.Should().Be(4);
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCorrectPage_WhenRequestingMiddlePage()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 10);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 2, PageSize = 3 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(3);
        result.Value.Page.Should().Be(2);
        result.Value.TotalCount.Should().Be(10);
        result.Value.TotalPages.Should().Be(4);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnCorrectPage_WhenRequestingLastPage()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 10);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 4, PageSize = 3 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Page.Should().Be(4);
        result.Value.TotalCount.Should().Be(10);
        result.Value.TotalPages.Should().Be(4);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenRequestingPageBeyondAvailable()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 5);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 10, PageSize = 3 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Page.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoProductsExist()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 10 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_ShouldEnforceMaxPageSize_WhenRequestingLargePageSize()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 150);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 200 }; //exceeded max

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(100); //capped at ma
        result.Value.PageSize.Should().Be(100);
        result.Value.TotalCount.Should().Be(150);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllItems_WhenPageSizeExceedsTotalCount()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 5);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 100 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(5);
        result.Value.TotalPages.Should().Be(1);
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Theory]
    [InlineData("name", 1)]
    [InlineData("name_desc", 20)]
    [InlineData("price", 1)]
    [InlineData("price_desc", 20)]
    public async Task GetAllAsync_ShouldApplySorting_WhenSortParameterProvided(string sortBy, int expectedFirstId)
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 20);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 10, Sort = sortBy };
        
        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);
        
        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(10);
        result.Value.Items.First().Id.Should().Be(expectedFirstId);
    }
    
    [Fact]
    public async Task GetAllAsync_ShouldCalculateTotalPagesCorrectly_WhenItemsDivideEvenly()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 20);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 5 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPages.Should().Be(4);
    }
    
    [Fact]
    public async Task GetAllAsync_ShouldCalculateTotalPagesCorrectly_WhenItemsDoNotDivideEvenly()
    {
        //Arrange
        var context = TestDbContextFactory.CreateInMemoryDbContext();
        await ProductHelper.SeedProducts(context, count: 23);

        var productService = new ProductService(context, ProductHelper.CreateValidator, ProductHelper.UpdateValidator);
        var paginationParams = new PaginationParams { Page = 1, PageSize = 5 };

        //Act
        var result = await productService.GetAllAsync(paginationParams, CancellationToken.None);

        //Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalPages.Should().Be(5);
        
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