namespace OrdersApi.Application.Products.Models;

public class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;
}