using OrdersApi.Application.Common;
using OrdersApi.Domain.Enums;

namespace OrdersApi.Application.Orders.Models;

public class OrderFilters : PaginationParams
{
    public OrderStatus? Status { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    
}