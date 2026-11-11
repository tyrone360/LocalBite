
using LocalBite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LocalBite.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Challenge();

        var orders = await _context.Orders
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.MenuItem)
            .Where(order => order.Customer != null &&
                            order.Customer.IdentityUserId == userId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return View(orders);
    }
}
