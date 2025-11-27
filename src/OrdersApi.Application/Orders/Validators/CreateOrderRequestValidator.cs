using FluentValidation;
using OrdersApi.Application.Orders.Models;

namespace OrdersApi.Application.Orders.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("CustomerId must be greater than 0.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Order must have at least one line item.")
            .Must(lines => lines is { Count: > 0 })
            .WithMessage("Order must have at least one line item.");

        RuleForEach(x => x.Lines).SetValidator(new CreateOrderLineRequestValidator());
    }
}

public class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("ProductId must be greater than 0.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThan(10000).WithMessage("Quantity must not exceed 10,000.");
    }
}