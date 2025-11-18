using OrdersApi.Application.Common;
using OrdersApi.Application.Orders.Models;

namespace OrdersApi.Application.Orders;

public interface IOrderService
{
    Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
    Task<Result<OrderResponse>> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Result<bool>> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken);
}