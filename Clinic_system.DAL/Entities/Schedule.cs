
namespace Clinic_System;

public class Schedule : BaseEntity
{
    public DateTime Day { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }

    public ScheduleStatus ScheduleStatus { get; set; }
    public Appointment Appointment { get; set; } = null!;
}