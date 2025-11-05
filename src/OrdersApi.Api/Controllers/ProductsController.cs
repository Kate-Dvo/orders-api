using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.DTOs;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController(OrdersDbContext context, ILogger<ProductsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProducts()
    {
        try
        {
            var products = await context.Products.Select(p => new ProductResponse
                {
                    Id = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    Price = p.Price,
                    IsActive = p.IsActive
                })
                .ToListAsync();
            return Ok(products);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, "Failed to extract products");
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetProduct(int id)
    {
        try
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { Message = $"Product with id {id} not found" });
            }

            var response = new ProductResponse
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Price = product.Price,
                IsActive = product.IsActive
            };
            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e.Message, "Failed to extract products");
            throw;
        }
    }

    //POST api/v1/products
    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct(CreateProductRequest request)
    {
        try
        {
            if (await context.Products.AnyAsync(p => p.Sku == request.Sku))
            {
                return Conflict(new { Message = $"Product with sku '{request.Sku}' already exists" });
            }

            var product = new Product
            {
                Sku = request.Sku,
                Name = request.Name,
                Price = request.Price,
                IsActive = request.IsActive
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            var response = new ProductResponse
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                Price = product.Price,
                IsActive = product.IsActive
            };

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
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
            var product = await context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { Message = $"Product with id {id} not found" });
            }

            if (await context.Products.AnyAsync(p => p.Sku == request.Sku && p.Id != id))
            {
                return Conflict(new { Message = $"Product with SKU {request.Sku} already exists" });
            }

            product.Sku = request.Sku;
            product.Name = request.Name;
            product.Price = request.Price;
            product.IsActive = request.IsActive;

            await context.SaveChangesAsync();

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
            var product = await context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { Message = $"Product with id {id} not found" });
            }

            context.Products.Remove(product);
            await context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete product with id {Id}", id);
            throw;
        }
    }
}