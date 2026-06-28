using Microsoft.AspNetCore.Identity;


namespace Clinic_System;

public class User : IdentityUser 
{
    public required string FullName { get; set; }

    public string? ProfileImage { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Doctor? DoctorProfile { get; set; }

    public Patient? PatientProfile { get; set; }
}
