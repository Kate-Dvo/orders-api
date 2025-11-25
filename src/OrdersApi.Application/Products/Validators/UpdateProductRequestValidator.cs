using FluentValidation;
using OrdersApi.Application.Products.Models;

namespace OrdersApi.Application.Products.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Sku is required.")
            .MaximumLength(50).WithMessage("Sku must not exceed 50 characters.")
            .MinimumLength(5).WithMessage("Sku must be at least 5 characters.")
            .Matches(@"^[A-Z0-9-]+$").WithMessage("SKU must contain only uppercase letters, numbers, and hyphens");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.")
            .MinimumLength(3).WithMessage("Name must be at least 3 characters.");
        
        RuleFor(x=> x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than or equals to 0.")
            .LessThan(1000000).WithMessage("Price must be less than or equal to 1,000,000.");
    }
}