namespace OrdersApi.Application.Orders.Models;

public class CreateOrderV2Request
{
    public int ClientId { get; init; }
    public List<CreateOrderLineRequest> Lines { get; init; } = [];
}