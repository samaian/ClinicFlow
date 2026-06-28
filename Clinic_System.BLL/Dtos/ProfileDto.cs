using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Clinic_System;

public class ProfileViewModel
{
    public DateTime DateOfBirth { get; set; }
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }
    [DataType(DataType.Password)]
    public string? Password { get; set; }    
}
