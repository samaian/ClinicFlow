using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class ChangePasswordDto
{
    [Required]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;


    [Required]
    [MinLength(6)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;


    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}