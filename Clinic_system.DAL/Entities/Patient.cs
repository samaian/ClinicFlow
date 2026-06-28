

namespace Clinic_System;

public class Patient : BaseEntity
{
   
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public string LastDiagnosis { get; set; } = string.Empty;
    public string MedicalHistory { get; set; } = string.Empty;
    public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    public List<Review> Reviews { get; set; } = new List<Review>();

    
}