using System.ComponentModel.DataAnnotations;

namespace LocalBite.Models;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();

    [Required]
    public string OrderStatus { get; set; } = "Placed";

    [Required]
    public string PaymentMethod { get; set; } = "Cash on Delivery";

    public decimal TotalAmount { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}