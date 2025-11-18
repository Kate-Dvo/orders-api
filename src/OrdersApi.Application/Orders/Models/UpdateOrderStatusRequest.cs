using System.ComponentModel.DataAnnotations;
using OrdersApi.Domain.Enums;

namespace OrdersApi.Application.Orders.Models;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; set; }
}