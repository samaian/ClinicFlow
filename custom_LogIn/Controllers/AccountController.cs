using custom_LogIn.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;



namespace Clinic_System;

public class AccountController : Controller
{


    
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
       
        _authService = authService;
    }

    [HttpGet]
    public IActionResult LogIn(string returnUrl = "")
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }







    [HttpPost]
    public async Task<IActionResult> LogIn(LoginVM model, string returnUrl = "")
    {
       

        if (!ModelState.IsValid)
        {
            
            TempData["ErrorMessage"] = "Invalid login attempt.";
            ModelState.AddModelError(string.Empty,"invalid login attempt");
            return View(model);
        }
        var dto = new LoginDto
        {
            Email = model.Email,
            Password = model.Password,
            RememberMe = model.RememberMe
        };

        var response = await _authService.LoginAsync(dto, CookieAuthenticationDefaults.AuthenticationScheme);
        if(!response.Success)
        {
            TempData["ErrorMessage"] = response.Message;
            ModelState.AddModelError(string.Empty, response.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Login successful!";
        if (!string.IsNullOrEmpty(returnUrl) && (Url?.IsLocalUrl(returnUrl) ?? false))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GoogleLogin(string returnUrl = "")
    {
        var redirectUrl = Url.Action("GoogleResponse", "Account", new { returnUrl });

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };
        properties.SetParameter("prompt", "select_account");

        
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleResponse(string returnUrl = "")
    {
        var response = await _authService.GoogleLoginAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!response.Success)
        {
            TempData["ErrorMessage"] = response.Message;
            return RedirectToAction("Login");
        }

        TempData["SuccessMessage"] = response.Message;
        
        if (!string.IsNullOrEmpty(returnUrl) && (Url?.IsLocalUrl(returnUrl) ?? false))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }







    [HttpGet]
    public IActionResult Register(string returnUrl = "")
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Register(RegisterVM model, string returnUrl = "")
    {
       
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var dto = new RegisterDto
        {
            Name = model.Name,
            Email = model.Email,
            Password = model.Password,
            DateOfBirth = model.DateOfBirth,
            MedicalHistory = model.MedicalHistory
        };

        var response = await _authService.RegisterAsync(dto, returnUrl);

        
        if (response is null || !response.Success)
        {
            var message = response?.Message ?? "فشل في عملية التسجيل.";
            TempData["ErrorMessage"] = message;

            var errors = response?.Errors ?? new List<string>();

           
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    if (!string.IsNullOrEmpty(error))
                        ModelState.AddModelError(string.Empty, error);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, message);
            }

            return View(model);
        }

        
        TempData["SuccessMessage"] = response.Message;

       
        return RedirectToAction("LogIn", new { returnUrl = returnUrl });
    }





    public async Task<IActionResult> ConfirmEmail(string userId, string token, string returnUrl)
    {
      

        if (!string.IsNullOrEmpty(returnUrl))
            TempData["ReturnUrl"] = returnUrl;
        
        var result = await _authService.ConfirmEmailAsync(userId, token);

        if (result.Success)
        {
            TempData["SuccessMessage"] = result.Message;
        }
        else
        {
            TempData["ErrorMessage"] = result.Message;
        }


        
        return RedirectToAction("Login");
    }

    public async Task<IActionResult> CompleteRegister(string returnUrl)
    {
        return View();
    }


    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
     
}