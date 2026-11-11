
using LocalBite.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalBite.Controllers;

public class MenuController : Controller
{
    private readonly ApplicationDbContext _context;

    public MenuController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var menuItems = await _context.MenuItems
            .Where(item => item.IsAvailable)
            .OrderBy(item => item.Name)
            .ToListAsync();

        return View(menuItems);
    }
}
