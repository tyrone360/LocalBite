
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalBite.Controllers;

[Authorize(Roles = "Admin")]
public class AdminDashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
