using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Products.Models;

namespace OrdersApi.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<(bool, ProductResponse?)> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<(bool, ProductResponse?)> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}