using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class ChangeEmailViewModel
{
    [Required(ErrorMessage = "email is required")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Invalid email address")]
    public string NewEmail { get; set; } = null!;
}
