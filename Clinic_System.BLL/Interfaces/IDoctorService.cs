using Clinic_System;

public interface IDoctorService
{
    Task<Response<DoctorDashboardDto>> GetDashboardAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Response<DoctorProfileDto>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Response<bool>> UpdateProfileAsync(
        string userId,
        UpdateDoctorProfileDto dto,
        CancellationToken cancellationToken = default);

    Task<Response<DoctorDetailsDto>> GetDoctorByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Response<PagedResult<DoctorDto>>> SearchDoctorsAsync(
        int? specialtyId,
        string? name,
        decimal? maxPrice,
        PaginationParameters parameters,
        CancellationToken cancellationToken = default);
}