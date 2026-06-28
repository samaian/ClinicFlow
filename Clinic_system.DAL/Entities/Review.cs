

namespace Clinic_System;

public class Review : BaseEntity
{
    public int DoctorProfileId { get; set; }
    public int PatientId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public virtual Doctor DoctorProfile { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
}
