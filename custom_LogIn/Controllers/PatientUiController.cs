using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Clinic_System;

[Authorize(Roles = "Patient")]
public class PatientUiController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly UserManager<User> _userManager;
    private readonly IPatientService _patientService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<PatientUiController> _logger;

    public PatientUiController(
        IAppointmentService appointmentService,
        UserManager<User> userManager,
        IPatientService patientService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<PatientUiController> logger)
    {
        _appointmentService = appointmentService;
        _userManager = userManager;
        _patientService = patientService;
        _emailTemplateService = emailTemplateService;
        _emailService = emailService;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DASHBOARD
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<IActionResult> Dashboard(int pageNumber = 1)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _appointmentService.GetPatientAppointmensAsync(
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

    // ─────────────────────────────────────────────────────────────────────────
    // CANCEL APPOINTMENT – GET (confirmation page)
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> CancelAppointment(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid appointment ID.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _appointmentService.GetAppointmentByIdAsync(id, userId, HttpContext.RequestAborted);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        var appointment = result.Data!;
        if (appointment.Status == AppointmentStatus.Canceled)
        {
            TempData["Error"] = "This appointment is already cancelled.";
            return RedirectToAction(nameof(Dashboard));
        }
        if (appointment.Status == AppointmentStatus.Completed)
        {
            TempData["Error"] = "Completed appointments cannot be cancelled.";
            return RedirectToAction(nameof(Dashboard));
        }

        return View(new CancelAppointmentDto { AppointmentId = id });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CANCEL APPOINTMENT – POST
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAppointment(CancelAppointmentDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var result = await _appointmentService.CancelAppointmentAsync(
            dto.AppointmentId, userId, dto.Reason, HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(dto);
        }

        TempData["Success"] = "Your appointment has been cancelled successfully.";
        return RedirectToAction(nameof(Dashboard));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AVAILABLE TIME SLOTS (AJAX / partial)
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetTimeSlots(int doctorId, DateTime date)
    {
        if (doctorId <= 0)
            return BadRequest(new { error = "Invalid doctor ID." });

        if (date == default)
            return BadRequest(new { error = "A valid date is required." });

        var result = await _appointmentService.GetAvailableTimeSlotsAsync(
            doctorId, date, HttpContext.RequestAborted);

        if (!result.Success)
            return BadRequest(new { error = result.Message });

        return Json(result.Data);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROFILE – GET
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        var vm = new UpdateProfileViewModel
        {
            FullName = user.FullName,
            DateOfBirth = user.DateOfBirth,
            Email = user.Email
        };

        return View(vm);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PROFILE – POST
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return RedirectToAction("Login", "Account");

        user.FullName = vm.FullName;
        user.DateOfBirth = vm.DateOfBirth;
        user.Age = DateTime.Now.Year - vm.DateOfBirth.Year;

        var identityResult = await _userManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
        {
            foreach (var error in identityResult.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(vm);
        }

        var patientResult = await _patientService.UpdatePatientByUserId(user.Id, vm.MedicalHistory!);
        if (!patientResult.Success)
        {
            ModelState.AddModelError(string.Empty, patientResult.Message);
            return View(vm);
        }

        TempData["Success"] = "Profile updated successfully.";
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

    // ── Change Email GET ─────────────────────────────────────────────
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> ChangeEmail()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return RedirectToAction("Login", "Account");


        ViewBag.CurrentEmail = user.Email;

        return View(new ChangeEmailViewModel());
    }



    // ── Change Email POST ────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel dto)
    {
        var user = await _userManager.GetUserAsync(User);


        if (user == null)
            return RedirectToAction("Login", "Account");



        ViewBag.CurrentEmail = user.Email;


        if (!ModelState.IsValid)
            return View(dto);



        if (user.Email == dto.NewEmail)
        {
            ModelState.AddModelError(
                "",
                "The new email must be different from your current email."
            );

            return View(dto);
        }



        var token = await _userManager.GenerateChangeEmailTokenAsync(
            user,
            dto.NewEmail
        );


        var result = await _userManager.ChangeEmailAsync(
            user,
            dto.NewEmail,
            token
        );


        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(dto);
        }



        user.UserName = dto.NewEmail;


        await _userManager.UpdateAsync(user);



        // Send confirmation email
        try
        {
            var confirmToken =
                await _userManager.GenerateEmailConfirmationTokenAsync(user);


            var encodedToken =
                Uri.EscapeDataString(confirmToken);


            var link =
                $"{Request.Scheme}://{Request.Host}/Account/ConfirmEmail" +
                $"?userId={user.Id}&token={encodedToken}";


            var body =
                await _emailTemplateService.GetTemplateAsync(
                    EmailTemplatesConstants.confirmationEmailTemplete
                );


            if (!string.IsNullOrWhiteSpace(body))
            {
                body = body
                    .Replace("{{User}}", user.FullName)
                    .Replace("{{link}}", link);


                await _emailService.SendEmailAsync(
                    dto.NewEmail,
                    "Confirm your new email",
                    body,
                    CancellationToken.None
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending confirmation email");
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
