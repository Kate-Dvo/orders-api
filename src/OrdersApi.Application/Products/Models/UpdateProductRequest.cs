using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Application.Products.Models;

public class UpdateProductRequest
{
    [Required(ErrorMessage = "SKU is required")]
    [StringLength(200, ErrorMessage = "SKU cannot exceed 50 characters")]
    public string Sku { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Price is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }
    
    [Required]
    public bool IsActive { get; set; }
}