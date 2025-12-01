using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Common;
using OrdersApi.Application.Products;
using OrdersApi.Application.Products.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(
    IProductService productService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? sort,
        CancellationToken cancellationToken = default)
    {
        var paginationParams = new PaginationParams
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort
        };

        var result = await productService.GetAllAsync(paginationParams, cancellationToken);

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

        Response.Headers.Append("X-Page", result.Value!.Page.ToString());
        Response.Headers.Append("X-Total-Count", result.Value.TotalCount.ToString());
        Response.Headers.Append("X-Page-Size", result.Value.PageSize.ToString());
        Response.Headers.Append("X-Total-Pages", result.Value.TotalPages.ToString());

        return Ok(result.Value.Items);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponse?>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
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

    //POST api/v1/products
    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.CreateAsync(request, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetProduct), new { id = result.Value?.Id }, result.Value)
            : result.ErrorType switch
            {
                ResultErrorType.NotFound => NotFound(new { Message = result.Error }),
                ResultErrorType.Validation => BadRequest(new { Message = result.Error }),
                ResultErrorType.BusinessRule => Conflict(new { Message = result.Error }),
                ResultErrorType.Conflict => Conflict(new { Message = result.Error }),
                _ => StatusCode(500, new { Message = result.Error })
            };
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateAsync(id, request, cancellationToken);
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
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        var result = await productService.DeleteAsync(id, cancellationToken);

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