using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Common;
using OrdersApi.Application.Products;
using OrdersApi.Application.Products.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(
    IProductService productService,
    ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts(CancellationToken cancellationToken)
    {
        try
        {
            var result = await productService.GetAllAsync(cancellationToken);

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
            logger.LogError(e, "Failed to extract products");
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse?>> GetProduct(int id, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to extract products");
            throw;
        }
    }

    //POST api/v1/products
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e,
                "Failed to create product from request: Sku '{RequestSku}',  Name '{RequestName}', Price {RequestPrice}, Is active {RequestIsActive}"
                , request.Sku, request.Name, request.Price, request.IsActive);
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update product with id {Id}", id);
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete product with id {Id}", id);
            throw;
        }
    }
}