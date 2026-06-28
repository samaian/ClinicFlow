using System.ComponentModel.DataAnnotations;

namespace custom_LogIn.ViewModels;

public class RegisterVM
{
    [Required(ErrorMessage ="Name is required")]
    [MaxLength(200, ErrorMessage = "Name exceeded The maximum length")]
    [MinLength(2, ErrorMessage = "Name must be at least 2 characters long")]
    public string  Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage ="Password is required")]
    [StringLength(50,MinimumLength =8, ErrorMessage = "Password must be between 8 and 50 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage ="Password Confirmation is required")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
    [DataType(DataType.Text)]
    [StringLength(50, MinimumLength = 8, ErrorMessage = "field can't be empty and can't exceed 500 character")]
    public string MedicalHistory { get; set; } = string.Empty;
    
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }


}
