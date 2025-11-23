using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Common;
using OrdersApi.Application.Orders.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Domain.Enums;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Application.Orders;

public class OrderService(OrdersDbContext context) : IOrderService
{
    public async Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var line in request.Lines.Where(line => line.Quantity <= 0))
        {
            return Result<OrderResponse>.Failure($"Quantity must be > 0 for product {line.ProductId}",
                ResultErrorType.Validation);
        }

        var customerExist = await context.Customers.AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExist)
        {
            return Result<OrderResponse>.Failure(
                $"Customer with id {request.CustomerId} not found",
                ResultErrorType.Validation);
        }

        var productIds = request.Lines.Select(l => l.ProductId).ToHashSet();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var line in request.Lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
            {
                return Result<OrderResponse>.Failure(
                    $"Product with Id {line.ProductId} not found",
                    ResultErrorType.NotFound);
            }

            if (!product.IsActive)
            {
                return Result<OrderResponse>.Failure(
                    $"Product with id {product.Id} not active",
                    ResultErrorType.Validation);
            }
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var order = new Order
        {
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            Total = 0m
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
        var orderLines = new List<OrderLine>();
        var total = 0m;

        foreach (var line in request.Lines)
        {
            orderLines.Add(
                new OrderLine
                {
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = products[line.ProductId].Price
                });

            total += line.Quantity * products[line.ProductId].Price;
        }

        order.Total = total;
        context.OrderLines.AddRange(orderLines);
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = MapToOrderResponse(order, orderLines);

        return Result<OrderResponse>.Success(response);
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (order == null)
        {
            return Result<OrderResponse>.Failure($"Order with id {id} not found",
                ResultErrorType.NotFound);
        }

        var orderResponse = MapToOrderResponse(order, order.Lines.ToList());

        return Result<OrderResponse>.Success(orderResponse);
    }

    public async Task<Result<bool>> UpdateStatusAsync(int id, UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await context.Orders.FindAsync([id], cancellationToken);

        if (order == null)
        {
            return Result<bool>.Failure($"Order with id {id} not found", ResultErrorType.NotFound);
        }

        if (request.RowVersion != null && !order.RowVersion.SequenceEqual(request.RowVersion))
        {
            return Result<bool>.Failure(
                "The order was modified by another user. Please refresh and try again.",
                ResultErrorType.ConcurrencyConflict);
        }

        //Allowed transitions Pending -> Paid, Pending -> Canceled
        if (order.Status != OrderStatus.Pending)
        {
            return Result<bool>.Failure(
                $"Order with status {order.Status} can only transition from Pending status",
                ResultErrorType.BusinessRule);
        }

        if (request.Status is not (OrderStatus.Paid or OrderStatus.Cancelled))
        {
            return Result<bool>.Failure($"Invalid target status {request.Status}. Only Paid or Cancelled allowed",
                ResultErrorType.Validation);
        }

        order.Status = request.Status;
        await context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    private static OrderResponse MapToOrderResponse(Order order, List<OrderLine> orderLines)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            RowVersion = order.RowVersion,
            Lines = orderLines.Select(ol => new OrderLineResponse
            {
                ProductId = ol.ProductId,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice
            }).ToList()
        };
    }
}