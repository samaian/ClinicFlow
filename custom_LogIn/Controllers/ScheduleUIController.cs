using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic_System;

[Authorize(Roles = "Doctor")]
public class ScheduleUIController : Controller
{
    private readonly IScheduleService _scheduleService;

    public ScheduleUIController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    // ── List all slots ────────────────────────────────────────────────────────
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _scheduleService.GetDoctorSchedulesAsync(userId, HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(new List<ScheduleDto>());
        }

        return View(result.Data!);
    }

    // ── Create slot – GET ─────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create() => View(new CreateScheduleDto { Day = DateTime.Today });

    // ── Create slot – POST ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScheduleDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _scheduleService.CreateScheduleAsync(userId, dto, HttpContext.RequestAborted);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(dto);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ── Delete slot – POST ────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _scheduleService.DeleteScheduleAsync(userId, id, HttpContext.RequestAborted);

        if (!result.Success)
            TempData["Error"] = result.Message;
        else
            TempData["Success"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
