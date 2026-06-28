using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class CreateReviewDto
{
    [Required]
    public int DoctorId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }
    
    [StringLength(500)]
    public string? Comment { get; set; }
}
