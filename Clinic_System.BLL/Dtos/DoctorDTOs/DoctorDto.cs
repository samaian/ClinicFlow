
namespace Clinic_System;

public class DoctorDto
{
    public int ProfileId { get; set; }
    public int YearsOfExcperience { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string? ImageUrl { get; set; }
    public string? Bio { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
