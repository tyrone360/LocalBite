
using LocalBite.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LocalBite.Controllers;

public class AccountController : Controller
{
    private const string ProfilePhotoClaim = "ProfilePhoto";
    private const long MaximumPhotoSize = 2 * 1024 * 1024;

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IWebHostEnvironment _environment;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Find the account using its email, even if its username has changed.
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
            return RedirectToAction("Index", "Home");

        ModelState.AddModelError(
            string.Empty,
            "Invalid email or password.");

        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return RedirectToAction("Login");

        var claims = await _userManager.GetClaimsAsync(user);

        ViewBag.ProfilePhoto = claims
            .FirstOrDefault(c => c.Type == ProfilePhotoClaim)?.Value;

        ViewBag.Username = user.UserName;

        return View(user);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUsername(string username)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return RedirectToAction("Login");

        username = username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            TempData["ProfileMessage"] = "Please enter a username.";
            return RedirectToAction("Profile");
        }

        if (username.Length < 3 || username.Length > 30)
        {
            TempData["ProfileMessage"] =
                "Your username must be between 3 and 30 characters.";

            return RedirectToAction("Profile");
        }

        var result = await _userManager.SetUserNameAsync(user, username);

        if (!result.Succeeded)
        {
            TempData["ProfileMessage"] = string.Join(
                " ",
                result.Errors.Select(error => error.Description));

            return RedirectToAction("Profile");
        }

        // Refresh the signed-in account so the new username appears immediately.
        await _signInManager.RefreshSignInAsync(user);

        TempData["ProfileMessage"] = "Your username was updated successfully!";

        return RedirectToAction("Profile");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile photo)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return RedirectToAction("Login");

        if (photo == null || photo.Length == 0)
        {
            TempData["ProfileMessage"] = "Please choose a photo first.";
            return RedirectToAction("Profile");
        }

        if (photo.Length > MaximumPhotoSize)
        {
            TempData["ProfileMessage"] = "Your photo must be smaller than 2 MB.";
            return RedirectToAction("Profile");
        }

        var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();

        if (extension != ".jpg" &&
            extension != ".jpeg" &&
            extension != ".png" &&
            extension != ".webp")
        {
            TempData["ProfileMessage"] =
                "Please upload a JPG, PNG or WebP image.";

            return RedirectToAction("Profile");
        }

        if (!await HasValidImageSignature(photo, extension))
        {
            TempData["ProfileMessage"] =
                "That file does not appear to be a valid image.";

            return RedirectToAction("Profile");
        }

        var uploadFolder = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            "profile");

        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadFolder, fileName);
        var photoUrl = $"/uploads/profile/{fileName}";

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var existingClaim = claims.FirstOrDefault(
            claim => claim.Type == ProfilePhotoClaim);

        IdentityResult result;

        if (existingClaim != null)
        {
            result = await _userManager.ReplaceClaimAsync(
                user,
                existingClaim,
                new Claim(ProfilePhotoClaim, photoUrl));
        }
        else
        {
            result = await _userManager.AddClaimAsync(
                user,
                new Claim(ProfilePhotoClaim, photoUrl));
        }

        if (!result.Succeeded)
        {
            System.IO.File.Delete(filePath);

            TempData["ProfileMessage"] =
                "We couldn't save your profile photo. Please try again.";

            return RedirectToAction("Profile");
        }

        // Delete the previous photo only after the new one is saved.
        if (existingClaim != null &&
            !string.IsNullOrWhiteSpace(existingClaim.Value))
        {
            var oldFileName = Path.GetFileName(existingClaim.Value);
            var oldFilePath = Path.Combine(uploadFolder, oldFileName);

            if (System.IO.File.Exists(oldFilePath))
                System.IO.File.Delete(oldFilePath);
        }

        TempData["ProfileMessage"] = "Your profile photo was updated!";

        return RedirectToAction("Profile");
    }

    private static async Task<bool> HasValidImageSignature(
        IFormFile photo,
        string extension)
    {
        var header = new byte[12];

        await using var stream = photo.OpenReadStream();

        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));

        if (extension == ".jpg" || extension == ".jpeg")
        {
            return bytesRead >= 3 &&
                   header[0] == 0xFF &&
                   header[1] == 0xD8 &&
                   header[2] == 0xFF;
        }

        if (extension == ".png")
        {
            byte[] signature =
                { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            return bytesRead >= 8 &&
                   header.Take(8).SequenceEqual(signature);
        }

        if (extension == ".webp")
        {
            return bytesRead >= 12 &&
                   System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                   System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        }

        return false;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
