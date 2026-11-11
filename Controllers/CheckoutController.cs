
using System.Security.Claims;
using System.Text.Json;
using LocalBite.Data;
using LocalBite.Models;
using LocalBite.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalBite.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    private const string CartSessionKey = "LocalBiteCart";

    public CheckoutController(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private List<CartItem> GetCart()
    {
        var cartJson = HttpContext.Session.GetString(CartSessionKey);

        if (string.IsNullOrEmpty(cartJson))
            return new List<CartItem>();

        return JsonSerializer.Deserialize<List<CartItem>>(cartJson)
               ?? new List<CartItem>();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);

        var model = new CheckoutViewModel
        {
            FullName = customer?.FullName ?? string.Empty,
            Email = user.Email ?? customer?.Email ?? string.Empty,
            PhoneNumber = customer?.PhoneNumber ?? string.Empty,
            DeliveryAddress = customer?.DeliveryAddress ?? string.Empty
        };

        if (!GetCart().Any())
            return RedirectToAction("Index", "Cart");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var cart = GetCart();

        if (!cart.Any())
            return RedirectToAction("Index", "Cart");

        if (!ModelState.IsValid)
            return View("Index", model);

        // Always retrieve current menu prices from the database.
        var menuItemIds = cart.Select(item => item.MenuItemId).Distinct().ToList();

        var menuItems = await _context.MenuItems
            .Where(item => menuItemIds.Contains(item.Id) && item.IsAvailable)
            .ToDictionaryAsync(item => item.Id);

        if (menuItems.Count != menuItemIds.Count)
        {
            TempData["CartMessage"] =
                "One or more meals are no longer available. Please review your cart.";

            return RedirectToAction("Index", "Cart");
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.IdentityUserId == user.Id);

        if (customer == null)
        {
            customer = new Customer
            {
                IdentityUserId = user.Id,
                FullName = model.FullName,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                DeliveryAddress = model.DeliveryAddress
            };

            _context.Customers.Add(customer);
        }
        else
        {
            customer.FullName = model.FullName;
            customer.Email = model.Email;
            customer.PhoneNumber = model.PhoneNumber;
            customer.DeliveryAddress = model.DeliveryAddress;
        }

        var order = new Order
        {
            Customer = customer,
            OrderStatus = "Placed",
            PaymentMethod = model.PaymentMethod,
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var cartItem in cart)
        {
            var menuItem = menuItems[cartItem.MenuItemId];
            var quantity = Math.Clamp(cartItem.Quantity, 1, 99);

            order.OrderItems.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = quantity,
                UnitPrice = menuItem.Price
            });

            order.TotalAmount += menuItem.Price * quantity;
        }

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();

        // Empty the cart only after the order has been saved.
        HttpContext.Session.Remove(CartSessionKey);

        return RedirectToAction(
            nameof(Confirmation),
            new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
                .ThenInclude(item => item.MenuItem)
            .FirstOrDefaultAsync(o =>
                o.Id == id &&
                o.Customer != null &&
                o.Customer.IdentityUserId == userId);

        if (order == null)
            return NotFound();

        return View(order);
    }
}
