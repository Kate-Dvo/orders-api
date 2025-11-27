using OrdersApi.Application.Products.Validators;

namespace OrdersApi.UnitTests.Helpers;

public static class ProductHelper
{
    public static CreateProductRequestValidator CreateValidator => new();
    public static UpdateProductRequestValidator UpdateValidator => new();
}