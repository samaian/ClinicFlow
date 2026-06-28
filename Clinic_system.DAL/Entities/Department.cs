

namespace Clinic_System;

public class Department : BaseEntity
{

  
    public int SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; } 

    public int ClinicId { get; set; }
    public SmartClinic? Clinic { get; set; }

    public List<Doctor> Doctors { get; set; } = new List<Doctor>();
}