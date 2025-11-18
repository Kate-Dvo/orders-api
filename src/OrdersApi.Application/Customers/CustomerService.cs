using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Common;
using OrdersApi.Application.Customers.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Application.Customers;

public class CustomerService(OrdersDbContext context) : ICustomerService
{
    public async Task<Result<IEnumerable<CustomerResponse>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var customers = await context.Customers
            .AsNoTracking()
            .Select(c => MapToCustomerResponse(c))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<CustomerResponse>>.Success(customers);
    }

    public async Task<Result<CustomerResponse>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
        {
            return Result<CustomerResponse>.Failure($"Customer with id {id} not found", ResultErrorType.NotFound);
        }

        var customerResponse = MapToCustomerResponse(customer);

        return Result<CustomerResponse>.Success(customerResponse);
    }


    public async Task<Result<CustomerResponse>> CreateAsync(CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        if (await context.Customers.AnyAsync(c => c.Email == request.Email, cancellationToken))
        {
            return Result<CustomerResponse>.Failure($"Email {request.Email} already exists", ResultErrorType.Conflict);
        }

        var customer = new Customer
        {
            Name = request.Name,
            Email = request.Email,
            CreatedAt = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);

        return Result<CustomerResponse>.Success(MapToCustomerResponse(customer));
    }

    public async Task<Result<bool>> UpdateAsync(int id, UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
        {
            return Result<bool>.Failure($"Customer with id {id} not found", ResultErrorType.NotFound);
        }

        if (await context.Customers.AnyAsync(c => c.Email == request.Email, cancellationToken))
        {
            return Result<bool>.Failure($"Customer with email {request.Email} already exists",
                ResultErrorType.Conflict);
        }

        customer.Name = request.Name;
        customer.Email = request.Email;

        await context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
        {
            return Result<bool>.Failure($"Customer with id {id} not found", ResultErrorType.NotFound);
        }

        context.Customers.Remove(customer);
        await context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static CustomerResponse MapToCustomerResponse(Customer customer)
    {
        var customerResponse = new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            CreatedAt = customer.CreatedAt
        };
        return customerResponse;
    }
}