


namespace Clinic_System;

public class AppointmentDto
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string SpecialtyName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsPaid { get; set; }
    public decimal AmountPaid { get; set; }
    public int UpComingAppointments { get; set; }
    public int AppointmentsCount { get; set; }

    
    // Aliases for compatibility with different service mappings
    public DateTime AppointmentDate { get => Date; set => Date = value; }
    public DateTime AppointmentTime { get => StartTime; set => StartTime = value; }
}
