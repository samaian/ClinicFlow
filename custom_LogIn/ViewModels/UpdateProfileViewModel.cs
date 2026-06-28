using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Clinic_System;

public class UpdateProfileViewModel
{
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? ProfileImage { get; set; }
    public string? MedicalHistory { get; set; }
    public string? Email { get; set; }
}
