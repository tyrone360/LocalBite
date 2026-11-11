
using LocalBite.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalBite.Controllers;

[Authorize(Roles = "Admin")]
public class AdminOrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] AllowedStatuses =
    {
        "Placed",
        "Accepted",
        "Preparing",
        "Ready for delivery",
        "Out for delivery",
        "Delivered",
        "Cancelled"
    };

    public AdminOrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.MenuItem)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        int id,
        string status)
    {
        if (!AllowedStatuses.Contains(status))
            return BadRequest("Invalid order status.");

        var order = await _context.Orders
            .FirstOrDefaultAsync(order => order.Id == id);

        if (order == null)
            return NotFound();

        order.OrderStatus = status;

        await _context.SaveChangesAsync();

        TempData["AdminMessage"] =
            $"Order #{order.Id} updated to {status}.";

        return RedirectToAction(nameof(Index));
    }
}
