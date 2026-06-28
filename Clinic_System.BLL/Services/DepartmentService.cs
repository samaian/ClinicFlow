
using Microsoft.EntityFrameworkCore;

namespace Clinic_System;

public class DepartmentService : IDepartmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Response<List<DepartmentDto>>> GetAllClinicDepartmentsAsync(int clinicId, CancellationToken cancellationToken = default)
    {
        var departments = await _unitOfWork.Repository<Department>().Query()
            .Include(d => d.Specialty)
            .Where(d => d.ClinicId == clinicId)
            .OrderBy(d => d.ClinicId)

            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                SpecialtyId = d.SpecialtyId,
                ClinicId = d.ClinicId
            })
            .ToListAsync(cancellationToken);

        return Response<List<DepartmentDto>>.SuccessResponse(departments);
    }

    public async Task<Response<DepartmentDto>> AddDepartmentAsync(DepartmentDto dto, CancellationToken cancellationToken = default)
    {
        var department = new Department { SpecialtyId = dto.SpecialtyId, ClinicId = dto.ClinicId };
        await _unitOfWork.Repository<Department>().AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        dto.Id = department.Id;
        return Response<DepartmentDto>.SuccessResponse(dto, "Department added successfully.");
    }

  

    public async Task<Response<List<SpecialtyDto>>> GetAllSpecialtiesAsync(CancellationToken cancellationToken = default)
    {

        var specialties = await _unitOfWork.Repository<Specialty>().Query()
            
            .Select(s => new SpecialtyDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync(cancellationToken);
        return Response<List<SpecialtyDto>>.SuccessResponse(specialties);
    }

    public async Task<Response<SpecialtyDto>> AddSpecialtyAsync(SpecialtyDto dto, CancellationToken cancellationToken = default)
    {
        var specialty = new Specialty { Name = dto.Name };
        await _unitOfWork.Repository<Specialty>().AddAsync(specialty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        dto.Id = specialty.Id;
        return Response<SpecialtyDto>.SuccessResponse(dto, "Specialty added successfully.");
    }
}
