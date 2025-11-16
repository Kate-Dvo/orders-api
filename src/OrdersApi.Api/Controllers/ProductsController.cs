using Microsoft.AspNetCore.Mvc;
using OrdersApi.Application.Products;
using OrdersApi.Application.Products.Models;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(
    IProductService productService,
    ILogger<ProductsController> logger,
    CancellationToken cancellationToken = default) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts()
    {
        try
        {
            var products = await productService.GetAllAsync(cancellationToken);
            return Ok(products);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to extract products");
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse?>> GetProduct(int id)
    {
        try
        {
            var (isSucceed, product) = await productService.GetByIdAsync(id, cancellationToken);
            if (!isSucceed)
            {
                return NotFound(new { Message = $"Product with id {id} not found" });
            }

            return Ok(product);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to extract products");
            throw;
        }
    }

    //POST api/v1/products
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request)
    {
        try
        {
            var (isSucceed, product) = await productService.CreateAsync(request, cancellationToken);

            if (!isSucceed)
            {
                return ValidationProblem("Failed to create product");
            }

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
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
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductRequest request)
    {
        try
        {
            await productService.UpdateAsync(id, request, cancellationToken);

            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update product with id {Id}", id);
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            await productService.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete product with id {Id}", id);
            throw;
        }
    }
}