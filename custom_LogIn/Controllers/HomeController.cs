using Clinic_System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Clinic_System;

public class HomeController : Controller
{
    private readonly IDepartmentService _departmentService;
    public HomeController(IDepartmentService depatmentService)
    { 
    
        _departmentService = depatmentService;

    }
    public async Task<IActionResult> Index()
    {
        var specialties = await _departmentService.GetAllSpecialtiesAsync();
        ViewData["Specialties"] = specialties.Data;
        return View();
    }
    [Authorize]
    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult FAQ() => View();
    public IActionResult Terms() => View();

   

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
