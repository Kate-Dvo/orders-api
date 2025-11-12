using System.ComponentModel.DataAnnotations;
using OrdersApi.Domain.Enums;

namespace OrdersApi.Api.DTOs;

public class UpdateOrderStatusRequest
{
    [Required]
    public OrderStatus Status { get; set; }
}