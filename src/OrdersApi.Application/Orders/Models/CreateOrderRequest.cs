namespace OrdersApi.Application.Orders.Models;

public class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public List<CreateOrderLineRequest> Lines { get; set; } = new();
}

public class CreateOrderLineRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}