namespace Clinic_System;

public class TimeSlotDto
{
    public int ScheduleId { get; set; }
    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayTime => $"{StartTime:hh:mm tt} - {EndTime:hh:mm tt}";
}
