public class DoctorProfileDto
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Specialization { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    public decimal ConsultationFee { get; set; }

    public string? Bio { get; set; }

    public double AverageRating { get; set; }

    public int ReviewsCount { get; set; }
}