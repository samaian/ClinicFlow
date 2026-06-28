using System.ComponentModel.DataAnnotations;

namespace custom_LogIn.ViewModels
{
    public class VerifyEmailVM
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }
}
