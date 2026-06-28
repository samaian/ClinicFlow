using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Clinic_System;

[Authorize]
public class AppointmentUIController : Controller
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentUIController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    // ── Doctor: view own appointments ─────────────────────────────────────────
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> DoctorAppointments(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _appointmentService.GetDoctorAppointmentsAsync(
            userId,
            new PaginationParameters { PageNumber = pageNumber, PageSize = 10 },
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(new PagedResult<AppointmentDto>());
        }

        return View(result.Data!);
    }
}
