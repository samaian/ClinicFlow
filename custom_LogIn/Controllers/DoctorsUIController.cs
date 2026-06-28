using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace Clinic_System;

public class DoctorsUIController : Controller
{
    private readonly IDoctorService _doctorService;
    private readonly IReviewService _reviewService;
    private readonly IDepartmentService _departmentService;
    private readonly UserManager<User> _userManager;

    public DoctorsUIController(
    IDoctorService doctorService,
    IReviewService reviewService,
    IDepartmentService departmentService,
    UserManager<User> userManager)
    {
        _doctorService = doctorService;
        _reviewService = reviewService;
        _departmentService = departmentService;
        _userManager = userManager;
    }
    [Route("doctors")]
    public async Task<IActionResult> Index(int? specialtyId, string? name, decimal? maxPrice, int pageNumber = 1, int pageSize = 10)
    {
        var spectialtiesResult = await _departmentService.GetAllSpecialtiesAsync();

        var pagination = new PaginationParameters { PageNumber = pageNumber, PageSize = pageSize };
        var result = await _doctorService.SearchDoctorsAsync(specialtyId, name, maxPrice, pagination, HttpContext.RequestAborted);
        ViewData["Specialty"] = specialtyId;
        ViewData["Name"] = name;
        ViewData["MaxPrice"] = maxPrice;
        var model = new DoctorIndexVM
        {
            Specialties = spectialtiesResult.Data!,
            Doctors = result.Data!,
            SelectedSpecialtyId = specialtyId
        };
        return View(model);
    }
    [Route("doctors/{id}")]
    public async Task<IActionResult> Details(int id, int pageNumber = 1, int pageSize = 5)
    {
        var doctorResult = await _doctorService.GetDoctorByIdAsync(id, HttpContext.RequestAborted);
        if (!doctorResult.Success) return NotFound();
        var reviewsPaged = await _reviewService.GetDoctorReviewsAsync(id, new PaginationParameters
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        });
        ViewData["ReviewsPaged"] = reviewsPaged.Data; // من نوع PagedResult<ReviewDto>
        return View(doctorResult.Data);
    }
    [HttpPost]
    [Authorize(Roles = "Patient")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(CreateReviewDto dto, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _reviewService.AddReviewAsync(dto, userId!, cancellationToken);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction("Details", "DoctorsUI", new { id = dto.DoctorId });
        }

        TempData["Success"] = "Review added successfully";
        return RedirectToAction("Details", "DoctorsUI", new { id = dto.DoctorId });
    }


    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Dashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _doctorService.GetDashboardAsync(
            userId!,
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(new DoctorDashboardDto());
        }

        return View(result.Data);
    }

    [HttpGet]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _doctorService.GetProfileAsync(
            userId!,
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Dashboard));
        }

        var vm = new UpdateDoctorProfileDto
        {
            Specialization = result.Data!.Specialization,
            ExperienceYears = result.Data.ExperienceYears,
            ConsultationFee = result.Data.ConsultationFee,
            Bio = result.Data.Bio
        };

        ViewBag.Profile = result.Data;

        return View(vm);
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Profile(UpdateDoctorProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!ModelState.IsValid)
        {
            var profile = await _doctorService.GetProfileAsync(
                userId!,
                HttpContext.RequestAborted);

            ViewBag.Profile = profile.Data;

            
            return View(dto);
        }

        var result = await _doctorService.UpdateProfileAsync(
            userId!,
            dto,
            HttpContext.RequestAborted);

        if (!result.Success)
        {
            ModelState.AddModelError("", result.Message);
            var profile = await _doctorService.GetProfileAsync(
                userId!,
                HttpContext.RequestAborted);

            ViewBag.Profile = profile.Data;

            var vm = new UpdateDoctorProfileDto
            {
                Specialization = profile.Data!.Specialization,
                ExperienceYears = profile.Data.ExperienceYears,
                ConsultationFee = profile.Data.ConsultationFee,
                Bio = profile.Data.Bio
            };

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

        var user = await _userManager.FindByIdAsync(userId!);


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


        if (result.Succeeded)
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            TempData["Success"] =
                "Password changed successfully. Please login again.";

            return RedirectToAction(
                "Login",
                "Account"
            );
        }


        await HttpContext.SignOutAsync(
            IdentityConstants.ApplicationScheme
        );




        return RedirectToAction(
            "Login",
            "Account"
        );
    }
}