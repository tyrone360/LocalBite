
using System.ComponentModel.DataAnnotations;

namespace LocalBite.Models.ViewModels;

public class CheckoutViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    [Display(Name = "Delivery Address")]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    [RegularExpression(
        "^(Cash on Delivery|EFT)$",
        ErrorMessage = "Please select a valid payment method."
    )]
    [Display(Name = "Payment Method")]
    public string PaymentMethod { get; set; } = "Cash on Delivery";

    [StringLength(500)]
    [Display(Name = "Order Notes (Optional)")]
    public string? Notes { get; set; }
}
