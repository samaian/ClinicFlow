using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic_System;

[Authorize(Roles = "Admin")]
public class AdminUIController : Controller
{
    private readonly IAdminService _adminService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AdminUIController> _logger;

    public AdminUIController(
        IAdminService adminService,
        UserManager<User> userManager,
        ILogger<AdminUIController> logger)
    {
        _adminService = adminService;
        _userManager = userManager;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DASHBOARD
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Dashboard()
    {
        var result = await _adminService.GetDashboardAsync(HttpContext.RequestAborted);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(new AdminDashboardDto());
        }
        return View(result.Data!);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROFILE – GET
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _adminService.GetProfileAsync(userId, HttpContext.RequestAborted);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        var vm = new UpdateAdminProfileDto
        {
            FullName    = result.Data!.FullName,
            DateOfBirth = result.Data.DateOfBirth
        };
        ViewBag.AdminProfile = result.Data;
        return View(vm);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROFILE – POST 
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateAdminProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var profileResult = await _adminService.GetProfileAsync(userId, HttpContext.RequestAborted);
            ViewBag.AdminProfile = profileResult.Data;
            return View(dto);
        }

        var result = await _adminService.UpdateProfileAsync(userId, dto, HttpContext.RequestAborted);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            var profileResult = await _adminService.GetProfileAsync(userId, HttpContext.RequestAborted);
            ViewBag.AdminProfile = profileResult.Data;
            return View(dto);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordDto());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);


        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");


        var user = await _userManager.FindByIdAsync(userId);


        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            return View(dto);
        }


        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword
        );


        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(dto);
        }


        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        return RedirectToAction(
            "Login",
            "Account"
        );
    }

}
