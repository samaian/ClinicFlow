using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class ChangePasswordVM
{
   
    [Required(ErrorMessage = "CurrentPassword is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; } 
    [Required(ErrorMessage = "Password is required")]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 50 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; } 
    [Required(ErrorMessage = "Password Confirmation is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string? ConfirmNewPassword { get; set; } 
}
