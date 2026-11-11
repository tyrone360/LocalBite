
using System.ComponentModel.DataAnnotations;

namespace LocalBite.Models;

public class MenuItem
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Category { get; set; } = "Other";

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000)]
    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;

    public List<OrderItem> OrderItems { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
