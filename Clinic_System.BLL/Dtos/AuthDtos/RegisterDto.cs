using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_System;

public class RegisterDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string MedicalHistory { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}
