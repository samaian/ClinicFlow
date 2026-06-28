
using System.ComponentModel.DataAnnotations;

namespace Clinic_System;

public class Contact : BaseEntity
{

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public List<SmartClinic> Clinics { get; set; } = new List<SmartClinic>();
}