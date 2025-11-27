using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Application.Common;
using OrdersApi.Application.Customers.Models;
using OrdersApi.Domain.Entities;
using OrdersApi.Infrastructure.Data;

namespace OrdersApi.Application.Customers;

public class CustomerService(
    OrdersDbContext context,
    IValidator<CreateCustomerRequest> createValidator,
    IValidator<UpdateCustomerRequest> updateValidator) : ICustomerService
{
    public async Task<Result<PagedResult<CustomerResponse>>> GetAllAsync(PaginationParams paginationParams,
        CancellationToken cancellationToken)
    {
        var query = context.Customers
            .AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplySorting(query, paginationParams.Sort);

        var customers = await query.Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(c => MapToCustomerResponse(c))
            .ToListAsync(cancellationToken);


        var pageResult = new PagedResult<CustomerResponse>
        {
            TotalCount = totalCount,
            Items = customers,
            Page = paginationParams.Page,
            PageSize = paginationParams.PageSize
        };


        return Result<PagedResult<CustomerResponse>>.Success(pageResult);
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
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<CustomerResponse>.Failure(errors, ResultErrorType.Validation);
        }

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
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return Result<bool>.Failure(errors, ResultErrorType.Validation);
        }

        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer == null)
        {
            return Result<bool>.Failure($"Customer with id {id} not found", ResultErrorType.NotFound);
        }

        if (await context.Customers.AnyAsync(c => c.Email == request.Email && c.Id != id, cancellationToken))
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

    private IQueryable<Customer> ApplySorting(IQueryable<Customer> query, string? sort)
    {
        if (string.IsNullOrEmpty(sort))
        {
            return query.OrderBy(x => x.Id);
        }

        var sortParts = sort.Split('_');
        var field = sortParts[0].ToLower();
        var direction = sortParts.Length > 1 ? sortParts[1] : "asc";

        query = field switch
        {
            "id" => direction == "desc" ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id),
            "name" => direction == "desc" ? query.OrderByDescending(x => x.Name) : query.OrderBy(x => x.Name),
            "email" => direction == "desc" ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "createdat" => direction == "desc"
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            _ => query.OrderBy(x => x.Id)
        };

        return query;
    }
}