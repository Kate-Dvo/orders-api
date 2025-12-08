using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Common;
using OrdersApi.Application.Orders.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Domain.Enums;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Application.Orders;

public class OrderService(OrdersDbContext context, IValidator<CreateOrderRequest> createValidator) : IOrderService
{
    public async Task<Result<OrderResponse>> CreateAsync(CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<OrderResponse>.Failure(errors, ResultErrorType.Validation);
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
            Total = 0m,
            DiscountPercent = request.DiscountPercent
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        var orderLines = new List<OrderLine>();
        var subTotal = 0m;

        foreach (var line in request.Lines)
        {
            var product = products[line.ProductId];
            var lineTotal = product.Price * line.Quantity;
            subTotal += lineTotal;

            orderLines.Add(
                new OrderLine
                {
                    OrderId = order.Id,
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    UnitPrice = products[line.ProductId].Price
                });
        }

        context.OrderLines.AddRange(orderLines);
        order.SubTotal = subTotal;
        order.Total = subTotal;

        if (request.DiscountPercent is > 0)
        {
            var discountMultiplier = request.DiscountPercent.Value / 100;
            order.Total = subTotal * discountMultiplier;
        }

        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var response = MapToOrderResponse(order, orderLines);

        return Result<OrderResponse>.Success(response);
    }

    public async Task<Result<PagedResult<OrderResponse>>> GetAllAsync(OrderFilters filters,
        CancellationToken cancellationToken)
    {
        var query = context.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .AsQueryable();

        if (filters.Status.HasValue)
        {
            query = query.Where(o => o.Status == filters.Status.Value);
        }

        if (filters.CustomerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == filters.CustomerId.Value);
        }

        if (filters.DateFrom.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filters.DateFrom.Value);
        }

        if (filters.DateTo.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filters.DateTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, filters.Sort);

        var orders = await query
            .Skip((filters.Page - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        var orderResponses = orders.Select(o => MapToOrderResponse(o, o.Lines.ToList())).ToList();

        var pageResult = new PagedResult<OrderResponse>
        {
            Items = orderResponses,
            TotalCount = totalCount,
            Page = filters.Page,
            PageSize = filters.PageSize
        };

        return Result<PagedResult<OrderResponse>>.Success(pageResult);
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
            SubTotal = order.SubTotal,
            Total = order.Total,
            DiscountPercent = order.DiscountPercent,
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

    private static IQueryable<Order> ApplySorting(IQueryable<Order> query, string? sort)
    {
        if (string.IsNullOrEmpty(sort))
        {
            return query.OrderBy(o => o.Id);
        }

        var sortParts = sort.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var field = sortParts[0].ToLower();
        var direction = sortParts.Length > 1 ? sortParts[1] : "asc";

        query = field switch
        {
            "createdat" => direction == "desc"
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt),
            "total" => direction == "desc"
                ? query.OrderByDescending(o => (double)o.Total)
                : query.OrderBy(o => (double)o.Total),
            "status" => direction == "desc" ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
            "id" => direction == "desc" ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id),
            _ => query.OrderBy(o => o.Id)
        };
        return query;
    }
}