using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Common;
using OrdersApi.Application.Orders;
using OrdersApi.Application.Orders.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController(IOrderService orderService, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var line in request.Lines.Where(line => line.Quantity <= 0))
            {
                return BadRequest(new { Message = $"Quantity must be > 0 for product {line.ProductId}" });
            }

            var result = await orderService.CreateAsync(request, cancellationToken);

            return result.IsSuccess
                ? CreatedAtAction(nameof(GetOrderById), new { id = result.Value?.Id }, result.Value)
                : MapErrorToActionResult(result.ErrorType, result.Error);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create order for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, new { Message = "An error occured while creating order" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(int id, CancellationToken cancellationToken)
    {
        var result = await orderService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : MapErrorToActionResult(result.ErrorType, result.Error);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await orderService.UpdateStatusAsync(id, request, cancellationToken);

            return result.IsSuccess
                ? NoContent()
                : MapErrorToActionResult(result.ErrorType, result.Error);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update order status for id {Id}", id);
            throw;
        }
    }

    private ActionResult MapErrorToActionResult(ResultErrorType errorType, string? message)
    {
        return errorType switch
        {
            ResultErrorType.NotFound => NotFound(new { Message = message }),
            ResultErrorType.Validation => BadRequest(new { Message = message }),
            ResultErrorType.BusinessRule => Conflict(new { Message = message }),
            ResultErrorType.Conflict => Conflict(new { Message = message }),
            _ => StatusCode(500, new { Message = message })
        };
    }
}