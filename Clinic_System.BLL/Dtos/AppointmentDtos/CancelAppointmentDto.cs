using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class CancelAppointmentDto
{
    [Required]
    public int AppointmentId { get; set; }

    [Required(ErrorMessage = "Please provide a reason for cancellation.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 500 characters.")]
    public string Reason { get; set; } = string.Empty;
}
