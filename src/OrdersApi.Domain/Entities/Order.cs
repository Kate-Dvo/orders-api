using OrdersApi.Domain.Enums;

namespace OrdersApi.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal? DiscountPercent { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Total { get; set; }
    public byte[] RowVersion { get; set; } = [];

    //Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}