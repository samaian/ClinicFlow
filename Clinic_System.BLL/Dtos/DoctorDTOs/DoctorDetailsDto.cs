
namespace Clinic_System;

public class DoctorDetailsDto
{
    public int ProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int YearsOfExcperience { get; set; } = 1;
    public string SpecialtyName { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public double Rating { get; set; }
    public string Bio { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<AvailabilitySlotDto> Availabilities { get; set; } = new();
}

public class AvailabilitySlotDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
}
