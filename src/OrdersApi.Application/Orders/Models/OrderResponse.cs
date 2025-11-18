namespace OrdersApi.Application.Orders.Models;

public class OrderResponse
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderLineResponse> Lines { get; set; } = new ();
    
}

public class OrderLineResponse
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}