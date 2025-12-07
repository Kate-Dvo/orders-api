using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrdersApi.Api.Helpers;
using OrdersApi.Application.Common;
using OrdersApi.Application.Customers;
using OrdersApi.Application.Customers.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[ApiVersion(Consts.ApiVersion1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomerController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "RequireUserRole")]
    [EnableRateLimiting(Consts.FixedRateLimit)]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 1,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort
        };

        var result = await customerService.GetAllAsync(paginationParams, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => BadRequest(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
    }


    [HttpGet("{id}")]
    [Authorize(Policy = "RequireUserRole")]
    [EnableRateLimiting(Consts.FixedRateLimit)]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id, CancellationToken cancellationToken)
    {
        var result = await customerService.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => BadRequest(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting(Consts.SlidingRateLimit)]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.CreateAsync(request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetCustomer), new { id = result.Value?.Id }, result.Value)
            : result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => BadRequest(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting(Consts.SlidingRateLimit)]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.UpdateAsync(id, request, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => BadRequest(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting(Consts.SlidingRateLimit)]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken cancellationToken)
    {
        var result = await customerService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => BadRequest(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
    }
}