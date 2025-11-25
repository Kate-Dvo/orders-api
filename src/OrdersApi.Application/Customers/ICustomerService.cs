using OrdersApi.Application.Common;
using OrdersApi.Application.Customers.Models;

namespace OrdersApi.Application.Customers;

public interface ICustomerService
{
    Task<Result<PagedResult<CustomerResponse>>> GetAllAsync(
        PaginationParams paginationPrams,
        CancellationToken cancellationToken);

    Task<Result<CustomerResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken cancellationToken);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken);
}