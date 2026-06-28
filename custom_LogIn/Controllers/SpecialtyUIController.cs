using Microsoft.AspNetCore.Mvc;

namespace Clinic_System;

public class SpecialtiesUIController : Controller
{
    private readonly IDepartmentService _specialtyService;

    public SpecialtiesUIController(IDepartmentService specialtyService)
    {
        _specialtyService = specialtyService;
    }

   
    public async Task<IActionResult> Index()
    {
        var result = await _specialtyService.GetAllSpecialtiesAsync();
        return View(result.Data);
    }
}
