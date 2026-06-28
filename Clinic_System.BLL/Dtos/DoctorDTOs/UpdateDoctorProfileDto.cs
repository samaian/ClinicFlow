using System.ComponentModel.DataAnnotations;

public class UpdateDoctorProfileDto
{
    [Required]
    public string Specialization { get; set; } = string.Empty;

    [Range(0, 60)]
    public int ExperienceYears { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ConsultationFee { get; set; }

    public string? Bio { get; set; }
}