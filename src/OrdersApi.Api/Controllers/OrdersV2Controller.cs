using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrdersApi.Api.Helpers;
using OrdersApi.Application.Common;
using OrdersApi.Application.Orders;
using OrdersApi.Application.Orders.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[ApiVersion(Consts.ApiVersion2)]
[Route("api/v{version:apiVersion}/orders")]
[Authorize]
[EnableRateLimiting(Consts.SlidingRateLimit)]
public class OrdersV2Controller(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder(CreateOrderV2Request request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = new CreateOrderRequest
        {
            CustomerId = request.ClientId,
            Lines = request.Lines,
        };
        
        var result = await orderService.CreateAsync(serviceRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => BadRequest(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
        }

        return CreatedAtAction(nameof(OrdersController.GetOrderById), "Orders",
            new { id = result.Value!.Id, version = Consts.ApiVersion1 }, result.Value);
    }
}