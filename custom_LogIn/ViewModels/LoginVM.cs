using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class LoginVM
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember Me?")]
    public bool RememberMe { get; set; } = false;
}
