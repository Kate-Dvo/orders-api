using System.ComponentModel.DataAnnotations;

namespace OrdersApi.Api.DTOs;

public class CreateOrderRequest
{
    [Required] public int CustomerId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one order line is required")]
    public List<CreateOrderLineRequest> Lines { get; set; } = new();
}

public class CreateOrderLineRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }
}