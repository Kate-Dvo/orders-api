using OrdersApi.Application.Common;
using OrdersApi.Application.Products.Models;

namespace OrdersApi.Application.Products;

public interface IProductService
{
    Task<Result<PagedResult<ProductResponse>>> GetAllAsync(
        PaginationParams paginationParams,
        CancellationToken cancellationToken);

    Task<Result<ProductResponse?>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<ProductResponse?>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken);
}