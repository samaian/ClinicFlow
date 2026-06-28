

namespace Clinic_System;

public class CancellationRequest : BaseEntity
{
   
    public int AppointmentId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public CancellationRequestStatus Status { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
    public virtual User RequestedByUser { get; set; } = null!;
    public virtual User? ReviewedByAdmin { get; set; }
}
