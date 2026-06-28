
namespace Clinic_System;

public interface IAdminService
{
    Task<Response<AdminDashboardDto>> GetDashboardAsync(CancellationToken ct = default);
    Task<Response<AdminProfileDto>> GetProfileAsync(string userId, CancellationToken ct = default);
    Task<Response<bool>> UpdateProfileAsync(string userId, UpdateAdminProfileDto dto, CancellationToken ct = default);
}
