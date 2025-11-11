using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Api.DTOs;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomerController(OrdersDbContext context, ILogger<CustomerController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetCustomers()
    {
        try
        {
            var customers = await context.Customers
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    CreatedAt = c.CreatedAt,
                }).ToListAsync();

            return Ok(customers);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve customers");
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
    {
        try
        {
            var customer = await context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound(new { Message = $"Customer with id {id} not found" });
            }

            var response = new CustomerResponse
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                CreatedAt = customer.CreatedAt
            };

            return Ok(response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve customer with id {Id}", id);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
    {
        try
        {
            if (await context.Customers.AnyAsync(c => c.Email == request.Email))
            {
                return Conflict(new { Message = $"Customer with email {request.Email} already exists" });
            }

            var customer = new Customer
            {
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            context.Add(customer);
            await context.SaveChangesAsync();

            var response = new CustomerResponse
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                CreatedAt = customer.CreatedAt
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create customer from request: Name '{RequestName}', Email '{RequestEmail}'",
                request.Name, request.Email);
            throw;
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        try
        {
            var customer = await context.Customers.FindAsync(id);
            
            if (customer is null)
            {
                return NotFound(new { Message = $"Customer with id {id} not found" });
            }

            if (await context.Customers.AnyAsync(c => c.Email == request.Email && c.Id == id))
            {
                return Conflict(new { Message = $"Customer with email {request.Email} already exists" });
            }

            customer.Name = request.Name;
            customer.Email = request.Email;
            
            await context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update customer with id {id}", id);
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        try
        {
            var customer = await context.Customers.FindAsync(id);
            
            if (customer is null)
            {
                return NotFound(new { Message = $"Customer with id {id} not found" });
            }
            
            context.Customers.Remove(customer);
            await context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete customer with id {id}", id );
            throw;
        }
    }
}