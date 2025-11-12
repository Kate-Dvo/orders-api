using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.DTOs;
using OrdersApi.Domain.Entities;
using OrdersApi.Domain.Enums;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController(OrdersDbContext context, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderRequest request)
    {
        try
        {
            var customer = await context.Customers.FirstOrDefaultAsync(x => x.Id == request.CustomerId);
            if (customer == null)
            {
                return BadRequest(new { Message = $"Customer with id {request.CustomerId} not found" });
            }

            //Validate all products
            var productIds = request.Lines.Select(l => l.ProductId).ToHashSet();
            var products = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p);

            foreach (var line in request.Lines)
            {
                if (!products.TryGetValue(line.ProductId, out var product))
                {
                    return BadRequest(new { Message = $"Product with id {line.ProductId} not found" });
                }

                if (!product.IsActive)
                {
                    return BadRequest(new { Message = $"Product with id {line.ProductId} is inactive" });
                }

                if (line.Quantity <= 0)
                {
                    return BadRequest(new { Message = $"Quantity must be > 0 for product {line.ProductId}" });
                }
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            var order = new Order
            {
                CustomerId = request.CustomerId,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                Total = 0m
            };

            context.Orders.Add(order);
            await context.SaveChangesAsync();

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
            context.OrderLines.AddRange(orderLines); // why not async here?
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            var response = new OrderResponse
            {
                Id = order.Id,
                CustomerId = request.CustomerId,
                Status = order.Status.ToString(),
                Total = order.Total,
                CreatedAt = order.CreatedAt,
                Lines = orderLines.Select(ol => new OrderLineResponse
                {
                    ProductId = ol.ProductId,
                    Quantity = ol.Quantity,
                    UnitPrice = ol.UnitPrice
                }).ToList()
            };

            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create order for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, new { Message = "An error occured while creating order" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(int id)
    {
        var order = await context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound(new { Message = $"Order with id {id} not found" });
        }

        var response = new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            Total = order.Total,
            CreatedAt = order.CreatedAt,
            Lines = order.Lines.Select(ol => new OrderLineResponse
            {
                ProductId = ol.ProductId,
                Quantity = ol.Quantity,
                UnitPrice = ol.UnitPrice
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound(new { Message = $"Order with id {id} not found" });
            }

            //Allowed transitions Pending -> Paid, Pending -> Canceled
            if (order.Status != OrderStatus.Pending)
            {
                return Conflict(new
                {
                    Message = $"Order id {id} with status {order.Status} cannot transition to {request.Status}"
                });
            }

            if (request.Status is not (OrderStatus.Paid or OrderStatus.Cancelled))
            {
                return Conflict(new { Message = $"Order id {order.Id} cannot transition to {request.Status}" });
            }

            order.Status = request.Status;
            await context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update order status for id {id}", id);
            throw;
        }
    }
}