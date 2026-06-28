
namespace Clinic_System;

public class Appointment : BaseEntity
{
    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int ScheduleId { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public bool IsPaid { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Patient Patient { get; set; } = null!;

    public Doctor Doctor { get; set; } = null!;

    public Schedule Schedule { get; set; } = null!;

    public Payment? Payment { get; set; }
    


}
