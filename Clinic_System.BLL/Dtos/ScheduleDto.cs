using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class ScheduleDto
{
    public int Id { get; set; }
    public DateTime Day { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ScheduleStatus Status { get; set; }
    public bool HasAppointment { get; set; }
    public string DisplayTime => $"{StartTime:hh:mm tt} – {EndTime:hh:mm tt}";
}

public class CreateScheduleDto
{
    [Required(ErrorMessage = "Date is required.")]
    public DateTime Day { get; set; }

    [Required(ErrorMessage = "Start time is required.")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "End time is required.")]
    public DateTime EndTime { get; set; }
}
