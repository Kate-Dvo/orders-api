using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrdersApi.Api.Helpers;
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
    [EnableRateLimiting(Consts.FixedRateLimit)]
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, NoStore = false)]
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
    [EnableRateLimiting(Consts.FixedRateLimit)]
    public async Task<ActionResult<ProductResponse?>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var result = await productService.GetByIdAsync(id, cancellationToken);
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

        var product = result.Value;

        var raw = $"{product?.Id}|{product?.Sku}|{product?.Name}|{product?.Price}|{product?.IsActive}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var etag = $"\"{Convert.ToBase64String(hashBytes)}\"";

        if (Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch) &&
            string.Equals(ifNoneMatch, etag, StringComparison.Ordinal))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = etag;

        return Ok(result.Value);
    }

    //POST api/v1/products
    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    [EnableRateLimiting(Consts.SlidingRateLimit)]
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
    [EnableRateLimiting(Consts.SlidingRateLimit)]
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
    [EnableRateLimiting(Consts.SlidingRateLimit)]
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