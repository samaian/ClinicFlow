using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class DoctorService : IDoctorService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(IUnitOfWork unitOfWork, ILogger<DoctorService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response<PagedResult<DoctorDto>>> SearchDoctorsAsync(
        int? specialtyId, string? name, decimal? maxPrice,
        PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        if (parameters == null || parameters.PageNumber < 1 || parameters.PageSize < 1)
            return Response<PagedResult<DoctorDto>>.FailureResponse("Invalid pagination parameters.");

        if (maxPrice.HasValue && maxPrice.Value < 0)
            return Response<PagedResult<DoctorDto>>.FailureResponse("Maximum price cannot be negative.");

        try
        {
            var query = _unitOfWork.Repository<Doctor>()
                .Query()
                .Include(p => p.User)
                .Include(p => p.Department)
                    .ThenInclude(d => d.Specialty)
                .AsNoTracking();

            if (specialtyId.HasValue)
                query = query.Where(p => p.Department!.Specialty!.Id == specialtyId);

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(p => p.User.FullName.Contains(name.Trim()));

            if (maxPrice.HasValue)
                query = query.Where(p => p.ConsultationFee <= maxPrice.Value);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(p => new DoctorDto
                {
                    ProfileId = p.Id,
                    FullName = p.User.FullName,
                    SpecialtyName = p.Department!.Specialty!.Name,
                    ConsultationFee = p.ConsultationFee,
                    AverageRating = _unitOfWork.Repository<Review>()
                        .Query()
                        .Where(r => r.DoctorProfileId == p.Id)
                        .Average(r => (double?)r.Rating) ?? 0.0,
                    Bio = p.Bio
                })
                .ToListAsync(cancellationToken);

            return Response<PagedResult<DoctorDto>>.SuccessResponse(new PagedResult<DoctorDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SearchDoctorsAsync was cancelled.");
            return Response<PagedResult<DoctorDto>>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching doctors.");
            return Response<PagedResult<DoctorDto>>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }

    public async Task<Response<DoctorDetailsDto>> GetDoctorByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            return Response<DoctorDetailsDto>.FailureResponse("Invalid doctor ID.");

        try
        {
            var profile = await _unitOfWork.Repository<Doctor>()
                .Query()
                .Include(p => p.User)
                .Include(p => p.Schedules)
                    .ThenInclude(s => s.Appointment)
                .Include(p => p.Department)
                    .ThenInclude(d => d.Specialty)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (profile == null)
                return Response<DoctorDetailsDto>.FailureResponse("Doctor not found.");

            var today = DateTime.UtcNow.Date;

            var availabilities = profile.Schedules
                .Where(s => s.ScheduleStatus == ScheduleStatus.Avaliable && s.Day.Date >= today)
                .Select(s => new AvailabilitySlotDto
                {
                    Id = s.Id,
                    Date = s.Day,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsAvailable = s.Appointment == null
                               || s.Appointment.Status == AppointmentStatus.Canceled
                })
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .ToList();

            var averageRating = await _unitOfWork.Repository<Review>()
                .Query()
                .Where(r => r.DoctorProfileId == id)
                .AverageAsync(r => (double?)r.Rating, cancellationToken) ?? 0.0;

            var dto = new DoctorDetailsDto
            {
                ProfileId = profile.Id,
                FullName = profile.User?.FullName ?? "Unknown Doctor",
                SpecialtyName = profile.Department?.Specialty?.Name ?? "General",
                ConsultationFee = profile.ConsultationFee,
                YearsOfExcperience = profile.ExperienceYears,
                Bio = profile.Bio ?? string.Empty,
                Rating = averageRating,
                Availabilities = availabilities
            };

            return Response<DoctorDetailsDto>.SuccessResponse(dto);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetDoctorByIdAsync cancelled. DoctorId={Id}", id);
            return Response<DoctorDetailsDto>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching doctor {DoctorId}", id);
            return Response<DoctorDetailsDto>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }
    public async Task<Response<Doctor>> GetDoctorByUserId(string userId)
    {
        var doctor = await _unitOfWork.Repository<Doctor>().Query()
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor is null)
            return Response<Doctor>.FailureResponse("Doctor profile not found.");

        return Response<Doctor>.SuccessResponse(doctor);
    }
    public async Task<Response<bool>> UpdateDoctorProfileByUserId(string userId, string specialization, int experienceYears, decimal consultationFee, string? bio)
    {
        var doctorRepo = _unitOfWork.Repository<Doctor>();

        var doctor = await doctorRepo.Query()
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor is null)
            return Response<bool>.FailureResponse("Doctor profile not found.");

        doctor.DoctorSpecialization = specialization;
        doctor.ExperienceYears = experienceYears;
        doctor.ConsultationFee = consultationFee;
        doctor.Bio = bio;

        doctorRepo.Update(doctor);
        await _unitOfWork.SaveChangesAsync();

        return Response<bool>.SuccessResponse(true);
    }

    public async Task<Response<DoctorDashboardDto>> GetDashboardAsync(
    string userId,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var doctor = await _unitOfWork.Repository<Doctor>()
                .Query()
                .Include(d => d.User)
                .Include(d => d.Reviews)
                    .ThenInclude(r => r.Patient)
                        .ThenInclude(p => p.User)
                .Include(d => d.Schedules)
                    .ThenInclude(s => s.Appointment)
                        .ThenInclude(a => a.Patient)
                            .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

            if (doctor == null)
                return Response<DoctorDashboardDto>.FailureResponse("Doctor not found.");

            var appointments = doctor.Schedules
                .Where(s => s.Appointment != null)
                .Select(s => s.Appointment!)
                .ToList();

            var dto = new DoctorDashboardDto
            {
                FullName = doctor.User.FullName,
                Specialization = doctor.DoctorSpecialization,

                TotalAppointments = appointments.Count,

                PendingAppointments =
                    appointments.Count(a => a.Status == AppointmentStatus.Pending),

                ConfirmedAppointments =
                    appointments.Count(a => a.Status == AppointmentStatus.Confirmed),

                CompletedAppointments =
                    appointments.Count(a => a.Status == AppointmentStatus.Completed),

                CancelledAppointments =
                    appointments.Count(a => a.Status == AppointmentStatus.Canceled),

                TotalPatients =
                    appointments.Select(a => a.PatientId).Distinct().Count(),

                TotalRevenue =
                    appointments
                        .Where(a => a.IsPaid)
                        .Sum(a => doctor.ConsultationFee),

                ReviewsCount = doctor.Reviews.Count,

                AverageRating = doctor.Reviews.Any()
                    ? doctor.Reviews.Average(r => r.Rating)
                    : 0,

                RecentAppointments = appointments
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .Select(a => new RecentAppointmentDto
                    {
                        Id = a.Id,
                        PatientName = a.Patient.User.FullName,
                        Date = a.CreatedAt,
                        Status = a.Status,
                        IsPaid = a.IsPaid
                    })
                    .ToList(),

                RecentReviews = doctor.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new RecentReviewDto
                    {
                        PatientName = r.Patient.User.FullName,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList()
            };

            return Response<DoctorDashboardDto>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading doctor dashboard");

            return Response<DoctorDashboardDto>.FailureResponse(
                "Something went wrong.");
        }
    }


    public async Task<Response<DoctorProfileDto>> GetProfileAsync(
    string userId,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var doctor = await _unitOfWork.Repository<Doctor>()
                .Query()
                .Include(d => d.User)
                .Include(d => d.Reviews)
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

            if (doctor == null)
                return Response<DoctorProfileDto>.FailureResponse("Doctor not found.");

            var dto = new DoctorProfileDto
            {
                FullName = doctor.User.FullName,
                Email = doctor.User.Email!,
                DateOfBirth = doctor.User.DateOfBirth,
                Specialization = doctor.DoctorSpecialization,
                ExperienceYears = doctor.ExperienceYears,
                ConsultationFee = doctor.ConsultationFee,
                Bio = doctor.Bio,
                ReviewsCount = doctor.Reviews.Count,
                AverageRating = doctor.Reviews.Any()
                    ? doctor.Reviews.Average(r => r.Rating)
                    : 0
            };

            return Response<DoctorProfileDto>.SuccessResponse(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading doctor profile");
            return Response<DoctorProfileDto>.FailureResponse("Something went wrong.");
        }
    }

    public async Task<Response<bool>> UpdateProfileAsync(
    string userId,
    UpdateDoctorProfileDto dto,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var doctor = await _unitOfWork.Repository<Doctor>()
                .Query()
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

            if (doctor == null)
                return Response<bool>.FailureResponse("Doctor not found.");

            doctor.DoctorSpecialization = dto.Specialization;
            doctor.ExperienceYears = dto.ExperienceYears;
            doctor.ConsultationFee = dto.ConsultationFee;
            doctor.Bio = dto.Bio;

            _unitOfWork.Repository<Doctor>().Update(doctor);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Response<bool>.SuccessResponse(true, "Profile updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor profile");
            return Response<bool>.FailureResponse("Something went wrong.");
        }
    }
}

