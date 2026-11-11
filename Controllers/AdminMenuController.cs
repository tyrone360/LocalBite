
using LocalBite.Data;
using LocalBite.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalBite.Controllers;

[Authorize(Roles = "Admin")]
public class AdminMenuController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminMenuController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var menuItems = await _context.MenuItems
            .OrderBy(item => item.Name)
            .ToListAsync();

        return View(menuItems);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Price,ImageUrl,IsAvailable")]
        MenuItem menuItem)
    {
        if (!ModelState.IsValid)
            return View(menuItem);

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        TempData["AdminMenuMessage"] = "Menu item added successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);

        if (menuItem == null)
            return NotFound();

        return View(menuItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("Id,Name,Description,Price,ImageUrl,IsAvailable")]
        MenuItem menuItem)
    {
        if (id != menuItem.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(menuItem);

        _context.MenuItems.Update(menuItem);
        await _context.SaveChangesAsync();

        TempData["AdminMenuMessage"] = "Menu item updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);

        if (menuItem == null)
            return NotFound();

        return View(menuItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var menuItem = await _context.MenuItems
            .Include(item => item.OrderItems)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (menuItem == null)
            return NotFound();

        if (menuItem.OrderItems.Any())
        {
            menuItem.IsAvailable = false;

            TempData["AdminMenuMessage"] =
                "This meal has previous orders, so it was hidden instead of deleted.";
        }
        else
        {
            _context.MenuItems.Remove(menuItem);

            TempData["AdminMenuMessage"] =
                "Menu item deleted successfully.";
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
