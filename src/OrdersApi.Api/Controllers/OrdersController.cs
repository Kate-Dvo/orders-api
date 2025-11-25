using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Common;
using OrdersApi.Application.Orders;
using OrdersApi.Application.Orders.Models;
using OrdersApi.Domain.Enums;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAllOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sort = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filters = new OrderFilters
            {
                Page = page,
                PageSize = pageSize,
                Sort = sort,
                Status = status,
                CustomerId = customerId,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var result = await orderService.GetAllAsync(filters, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return MapErrorToActionResult(result.ErrorType, result.Error);
            }
            
            Response.Headers.Append("X-Total-Count", result.Value?.TotalCount.ToString());
            Response.Headers.Append("X-Page", result.Value?.Page.ToString());
            Response.Headers.Append("X-Page-Size", result.Value?.PageSize.ToString());
            Response.Headers.Append("X-Total-Pages", result.Value?.TotalPages.ToString());
            
            return Ok(result.Value?.Items);
            
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get all orders");
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrderById(int id, CancellationToken cancellationToken)
    {
        var result = await orderService.GetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return MapErrorToActionResult(result.ErrorType, result.Error);
        }

        var rowVersion = result.Value?.RowVersion;

        if (rowVersion is not { Length: > 0 }) return Ok(result.Value);

        var etag = Convert.ToBase64String(rowVersion);
        Response.Headers.ETag = $"\"{etag}\"";

        return Ok(result.Value);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!Request.Headers.TryGetValue("If-Match", out var ifMatchValue))
            {
                return StatusCode(428, new
                {
                    Message = "If-Match header is required for updates",
                    Detail = "Include the ETag from GET request in If-Match header"
                });
            }

            var etagString = ifMatchValue.ToString().Trim('"');

            try
            {
                request.RowVersion = Convert.FromBase64String(etagString);
            }
            catch (FormatException)
            {
                return BadRequest(new { Message = "Invalid ETag format" });
            }

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
            ResultErrorType.ConcurrencyConflict => StatusCode(412, new { Message = message }),
            ResultErrorType.Conflict => Conflict(new { Message = message }),
            _ => StatusCode(500, new { Message = message })
        };
    }
}