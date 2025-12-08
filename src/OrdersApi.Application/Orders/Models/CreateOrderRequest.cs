namespace OrdersApi.Application.Orders.Models;

public class CreateOrderRequest
{
    public int CustomerId { get; init; }
    public List<CreateOrderLineRequest> Lines { get; init; } = [];
    public decimal? DiscountPercent { get; init; }
}

public class CreateOrderLineRequest
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}