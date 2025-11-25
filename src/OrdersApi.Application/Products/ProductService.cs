using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Common;
using OrdersApi.Application.Products.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Application.Products;

public class ProductService(OrdersDbContext context) : IProductService
{
    public async Task<Result<PagedResult<ProductResponse>>> GetAllAsync(PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var query = context.Products.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, paginationParams.Sort);

        var products = await query
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        var pageResult = new PagedResult<ProductResponse>
        {
            Items = products,
            TotalCount = totalCount,
            Page = paginationParams.Page,
            PageSize = paginationParams.PageSize
        };

        return Result<PagedResult<ProductResponse>>.Success(pageResult);
    }

    public async Task<Result<ProductResponse?>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            return Result<ProductResponse?>.Failure($"Product with id {id} was not found", ResultErrorType.NotFound);
        }

        var response = new ProductResponse
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            IsActive = product.IsActive
        };

        return Result<ProductResponse?>.Success(response);
    }

    public async Task<Result<ProductResponse?>> CreateAsync(CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            return Result<ProductResponse?>.Failure($"Product with SKU {request.Sku} already exist.",
                ResultErrorType.Conflict);
        }

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

        return Result<ProductResponse?>.Success(response);
    }

    public async Task<Result<bool>> UpdateAsync(int id, UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        if (await context.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id, cancellationToken))
        {
            return Result<bool>.Failure($"Product with SKU {request.Sku} already exist.", ResultErrorType.Conflict);
        }

        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            return Result<bool>.Failure($"Product with id {id} was not found", ResultErrorType.NotFound);
        }

        product.Sku = request.Sku;
        product.Name = request.Name;
        product.Price = request.Price;
        product.IsActive = request.IsActive;

        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            return Result<bool>.Failure($"Product with id {id} was not found", ResultErrorType.NotFound);
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sort)
    {
        if (string.IsNullOrEmpty(sort))
        {
            return query.OrderBy(p => p.Id);
        }

        var sortParts = sort.Split('_');
        var field = sortParts[0].ToLower();
        var direction = sortParts.Length > 1 ? sortParts[1] : "asc";

        query = field switch
        {
            "name" => direction == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => direction == "desc"
                ? query.OrderByDescending(p => (double)p.Price)
                : query.OrderBy(p => (double)p.Price),
            "sku" => direction == "desc" ? query.OrderByDescending(p => p.Sku) : query.OrderBy(p => p.Sku),
            "id" => direction == "desc" ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
            _ => query.OrderBy(p => p.Id)
        };
        return query;
    }
}