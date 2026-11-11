
using System.Text.Json;
using LocalBite.Data;
using LocalBite.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalBite.Controllers;

public class CartController : Controller
{
    private readonly ApplicationDbContext _context;
    private const string CartSessionKey = "LocalBiteCart";

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }

    private List<CartItem> GetCart()
    {
        var cartJson = HttpContext.Session.GetString(CartSessionKey);

        if (string.IsNullOrEmpty(cartJson))
            return new List<CartItem>();

        return JsonSerializer.Deserialize<List<CartItem>>(cartJson)
               ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString(
            CartSessionKey,
            JsonSerializer.Serialize(cart));
    }

    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int menuItemId, int quantity = 1)
    {
        var menuItem = await _context.MenuItems
            .FirstOrDefaultAsync(item =>
                item.Id == menuItemId && item.IsAvailable);

        if (menuItem == null)
        {
            TempData["CartMessage"] = "This meal is currently unavailable.";
            return RedirectToAction("Index", "Menu");
        }

        quantity = Math.Clamp(quantity, 1, 99);

        var cart = GetCart();

        var existingItem = cart.FirstOrDefault(
            item => item.MenuItemId == menuItemId);

        if (existingItem != null)
        {
            existingItem.Quantity = Math.Min(
                existingItem.Quantity + quantity, 99);
        }
        else
        {
            cart.Add(new CartItem
            {
                MenuItemId = menuItem.Id,
                Name = menuItem.Name,
                Price = menuItem.Price,
                ImageUrl = menuItem.ImageUrl,
                Quantity = quantity
            });
        }

        SaveCart(cart);

        TempData["CartMessage"] = $"{menuItem.Name} added to your cart!";

        return RedirectToAction("Index", "Menu");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int menuItemId, int quantity)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(
            product => product.MenuItemId == menuItemId);

        if (item != null)
        {
            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = Math.Min(quantity, 99);
        }

        SaveCart(cart);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int menuItemId)
    {
        var cart = GetCart();

        cart.RemoveAll(item => item.MenuItemId == menuItemId);

        SaveCart(cart);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);

        return RedirectToAction(nameof(Index));
    }
}
