
using System.ComponentModel.DataAnnotations;

namespace LocalBite.Models;

public class Customer
{
    public int Id { get; set; }

    // Links this customer record to an ASP.NET Identity account.
    [Required]
    public string IdentityUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string DeliveryAddress { get; set; } = string.Empty;

    public List<Order> Orders { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
