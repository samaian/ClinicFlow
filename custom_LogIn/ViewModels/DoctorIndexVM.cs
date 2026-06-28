namespace Clinic_System;

public class DoctorIndexVM
{

    public PagedResult<DoctorDto> Doctors { get; set; } = null!;
  

    public IEnumerable<SpecialtyDto> Specialties { get; set; }
        = Enumerable.Empty<SpecialtyDto>();

    public int? SelectedSpecialtyId { get; set; }
}
