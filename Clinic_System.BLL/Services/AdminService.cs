using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUnitOfWork unitOfWork, ILogger<AdminService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response<AdminDashboardDto>> GetDashboardAsync(CancellationToken ct = default)
    {
        try
        {
            var totalPatients = await _unitOfWork.Repository<Patient>().Query().CountAsync(ct);
            var totalDoctors = await _unitOfWork.Repository<Doctor>().Query().CountAsync(ct);
            var totalAppointments = await _unitOfWork.Repository<Appointment>().Query()
                                        .Where(a => !a.IsDeleted).CountAsync(ct);

            var totalRevenue = await _unitOfWork.Repository<Payment>().Query()
                                   .Where(p => p.Status == PaymentStatus.Completed)
                                   .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

           
            var statuses = await _unitOfWork.Repository<Appointment>().Query()
                .Where(a => !a.IsDeleted)
                .Select(a => a.Status)
                .ToListAsync(ct);

            var pending = statuses.Count(s => s == AppointmentStatus.Pending);
            var confirmed = statuses.Count(s => s == AppointmentStatus.Confirmed);
            var completed = statuses.Count(s => s == AppointmentStatus.Completed);
            var cancelled = statuses.Count(s => s == AppointmentStatus.Canceled);

            var recentAppointments = await _unitOfWork.Repository<Appointment>().Query()
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Schedule)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.Schedule.StartTime)
                .Take(10)
                .Select(a => new RecentAppointmentDto
                {
                    Id = a.Id,
                    PatientName = a.Patient.User.FullName,
                    DoctorName = a.Doctor.User.FullName,
                    Date = a.Schedule.StartTime,
                    Status = a.Status,
                    IsPaid = a.IsPaid
                })
                .ToListAsync(ct);

            var recentPayments = await _unitOfWork.Repository<Payment>().Query()
                .Include(p => p.Appointment)
                    .ThenInclude(a => a.Patient)
                    .ThenInclude(p => p.User)
                .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt != null)
                .OrderByDescending(p => p.PaidAt)
                .Take(8)
                .Select(p => new RecentPaymentDto
                {
                    Id = p.Id,
                    PatientName = p.Appointment.Patient.User.FullName,
                    Amount = p.Amount,
                    PaidAt = p.PaidAt!.Value,
                    Status = p.Status
                })
                .ToListAsync(ct);

            
            var apptFlat = await _unitOfWork.Repository<Appointment>().Query()
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Doctor).ThenInclude(d => d.Department).ThenInclude(dep => dep.Specialty)
                .Include(a => a.Payment)
                .Where(a => !a.IsDeleted)
                .Select(a => new
                {
                    a.DoctorId,
                    DoctorName = a.Doctor.User.FullName,
                    SpecialtyName = a.Doctor.Department != null && a.Doctor.Department.Specialty != null
                                        ? a.Doctor.Department.Specialty.Name
                                        : "General",
                    PaymentAmount = a.Payment != null ? (decimal?)a.Payment.Amount : null
                })
                .ToListAsync(ct);

            var topDoctors = apptFlat
                .GroupBy(a => new { a.DoctorId, a.DoctorName, a.SpecialtyName })
                .Select(g => new TopDoctorDto
                {
                    DoctorId = g.Key.DoctorId,
                    FullName = g.Key.DoctorName,
                    Specialty = g.Key.SpecialtyName,
                    AppointmentCount = g.Count(),
                    Revenue = g.Sum(a => a.PaymentAmount ?? 0m)
                })
                .OrderByDescending(d => d.AppointmentCount)
                .Take(5)
                .ToList();

            foreach (var doc in topDoctors)
            {
                doc.AverageRating = await _unitOfWork.Repository<Review>().Query()
                    .Where(r => r.DoctorProfileId == doc.DoctorId)
                    .AverageAsync(r => (double?)r.Rating, ct) ?? 0.0;
            }

            var sixMonthsAgo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5);

            var rawPayments = await _unitOfWork.Repository<Payment>().Query()
                .Where(p => p.Status == PaymentStatus.Completed
                         && p.PaidAt != null
                         && p.PaidAt >= sixMonthsAgo)
                .Select(p => new { p.PaidAt, p.Amount })
                .ToListAsync(ct);

            var monthlyRevenue = rawPayments
                .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyRevenueDto
                {
                    Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Revenue = g.Sum(p => p.Amount),
                    AppointmentCount = g.Count()
                })
                .ToList();

            return Response<AdminDashboardDto>.SuccessResponse(new AdminDashboardDto
            {
                TotalPatients = totalPatients,
                TotalDoctors = totalDoctors,
                TotalAppointments = totalAppointments,
                TotalRevenue = totalRevenue,
                PendingAppointments = pending,
                ConfirmedAppointments = confirmed,
                CompletedAppointments = completed,
                CancelledAppointments = cancelled,
                RecentAppointments = recentAppointments,
                RecentPayments = recentPayments,
                TopDoctors = topDoctors,
                MonthlyRevenue = monthlyRevenue
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetDashboardAsync cancelled.");
            return Response<AdminDashboardDto>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building admin dashboard. Message: {Message} | Inner: {Inner}",
                ex.Message, ex.InnerException?.Message);
            return Response<AdminDashboardDto>.FailureResponse(
                $"Dashboard error: {ex.Message}" +
                (ex.InnerException != null ? $" → {ex.InnerException.Message}" : ""));
        }
    }

    public async Task<Response<AdminProfileDto>> GetProfileAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<AdminProfileDto>.FailureResponse("User ID is required.");

        try
        {
            var user = await _unitOfWork.Repository<User>()
                .Query()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
                return Response<AdminProfileDto>.FailureResponse("Admin user not found.");

            return Response<AdminProfileDto>.SuccessResponse(new AdminProfileDto
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetProfileAsync cancelled. UserId={UserId}", userId);
            return Response<AdminProfileDto>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching admin profile for userId={UserId}", userId);
            return Response<AdminProfileDto>.FailureResponse("An unexpected error occurred.");
        }
    }

    public async Task<Response<bool>> UpdateProfileAsync(string userId, UpdateAdminProfileDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        if (dto is null)
            return Response<bool>.FailureResponse("Profile data is required.");

        try
        {
            var user = await _unitOfWork.Repository<User>()
                .Query()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
                return Response<bool>.FailureResponse("Admin user not found.");

            user.FullName = dto.FullName.Trim();
            user.DateOfBirth = dto.DateOfBirth;
            user.Age = DateTime.UtcNow.Year - dto.DateOfBirth.Year;
            _unitOfWork.Repository<User>().Update(user);
            await _unitOfWork.SaveChangesAsync(ct);

            return Response<bool>.SuccessResponse(true, "Profile updated successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("UpdateProfileAsync cancelled. UserId={UserId}", userId);
            return Response<bool>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin profile for userId={UserId}", userId);
            return Response<bool>.FailureResponse("An unexpected error occurred.");
        }
    }
}
