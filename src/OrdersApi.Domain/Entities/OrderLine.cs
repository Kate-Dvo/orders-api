namespace OrdersApi.Domain.Entities;

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    //Navigation properties
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}