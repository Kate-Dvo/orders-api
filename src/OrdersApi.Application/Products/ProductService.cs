using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Products.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Application.Products;

public class ProductService(OrdersDbContext context) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await context.Products
            .AsNoTracking()
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return products;
    }

    public async Task<(bool, ProductResponse?)> GetByIdAsync(int id,  CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync(id,  cancellationToken);
        var isFound = product != null;

        if (!isFound)
        {
            return (!isFound, null);
        }

        var response = new ProductResponse
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            IsActive = product.IsActive
        };

        return (isFound, response);
    }

    public async Task<(bool, ProductResponse?)> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Price = request.Price,
                IsActive = request.IsActive
            };

            context.Products.Add(product);
            await context.SaveChangesAsync(cancellationToken);

            var response = new ProductResponse
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Price = product.Price,
                IsActive = product.IsActive
            };

            return (true, response);
        }
        catch (DbUpdateException)
        {
            return (false, null);
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync(id, cancellationToken);

        var isFound = product != null;
        if (!isFound)
        {
            return !isFound;
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Price = request.Price;
        product.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return isFound;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var product = await context.Products.FindAsync(id, cancellationToken);

        if (product == null)
        {
            return false;
            //NotFound(new { Message = $"Product with id {id} not found" });
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}