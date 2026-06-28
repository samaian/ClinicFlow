
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic_System;

public interface IDepartmentService
{
    Task<Response<List<DepartmentDto>>> GetAllClinicDepartmentsAsync(int clinicId,CancellationToken cancellationToken = default);
    Task<Response<List<SpecialtyDto>>> GetAllSpecialtiesAsync(CancellationToken cancellationToken = default);
    Task<Response<DepartmentDto>> AddDepartmentAsync(DepartmentDto dto, CancellationToken cancellationToken = default);
    Task<Response<SpecialtyDto>> AddSpecialtyAsync(SpecialtyDto dto, CancellationToken cancellationToken = default);
}
