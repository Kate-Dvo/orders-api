namespace OrdersApi.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    //Navigation property
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}