using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Common;
using OrdersApi.Application.Customers;
using OrdersApi.Application.Customers.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomerController(ICustomerService customerService, ILogger<CustomerController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetCustomers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 1,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve customers");
            throw;
        }
    }


    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve customer with id {Id}", id);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create customer from request: Name '{RequestName}', Email '{RequestEmail}'",
                request.Name, request.Email);
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update customer with id {id}", id);
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete customer with id {id}", id);
            throw;
        }
    }
}