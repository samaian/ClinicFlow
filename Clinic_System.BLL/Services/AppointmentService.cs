using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class AppointmentService : IAppointmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(IUnitOfWork unitOfWork, ILogger<AppointmentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET PATIENT APPOINTMENTS (paginated)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<Response<PagedResult<AppointmentDto>>> GetPatientAppointmensAsync(
        string userId, PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<PagedResult<AppointmentDto>>.FailureResponse("User ID is required.");

        if (parameters is null)
            return Response<PagedResult<AppointmentDto>>.FailureResponse("Pagination parameters are required.");

        if (parameters.PageNumber < 1 || parameters.PageSize < 1)
            return Response<PagedResult<AppointmentDto>>.FailureResponse("Invalid pagination parameters.");

        try
        {
            var patientId = await _unitOfWork.Repository<Patient>()
                .Query()
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (patientId == 0)
                return Response<PagedResult<AppointmentDto>>.FailureResponse("Patient profile not found.");

            var query = _unitOfWork.Repository<Appointment>()
                .Query()
                .Include(b => b.Schedule)
                .Include(b => b.Doctor).ThenInclude(d => d.User)
                .Include(b => b.Patient).ThenInclude(p => p.User)
                .Where(b => b.PatientId == patientId && !b.IsDeleted)
                .OrderByDescending(b => b.Schedule.StartTime);

            var upcomingCount = await _unitOfWork.Repository<Appointment>()
                .Query()
                .Include(a => a.Schedule)
                .Where(a => a.PatientId == patientId
                         && !a.IsDeleted
                         && a.Schedule.StartTime > DateTime.UtcNow
                         && a.Status != AppointmentStatus.Canceled)
                .CountAsync(cancellationToken);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(b => new AppointmentDto
                {
                    Id = b.Id,
                    PatientName = b.Patient.User.FullName,
                    DoctorName = b.Doctor.User.FullName,
                    Date = b.Schedule.Day,
                    StartTime = b.Schedule.StartTime,
                    EndTime = b.Schedule.EndTime,
                    Status = b.Status,
                    ConsultationFee = b.Doctor.ConsultationFee,
                    IsPaid = b.IsPaid,
                    UpComingAppointments = upcomingCount,
                    AppointmentsCount = totalCount
                })
                .ToListAsync(cancellationToken);

            return Response<PagedResult<AppointmentDto>>.SuccessResponse(new PagedResult<AppointmentDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetPatientAppointmensAsync was cancelled for userId={UserId}", userId);
            return Response<PagedResult<AppointmentDto>>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointments for userId={UserId}", userId);
            return Response<PagedResult<AppointmentDto>>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET SINGLE APPOINTMENT BY ID
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<Response<AppointmentDto>> GetAppointmentByIdAsync(
        int appointmentId, string userId, CancellationToken cancellationToken = default)
    {
        if (appointmentId <= 0)
            return Response<AppointmentDto>.FailureResponse("Invalid appointment ID.");

        if (string.IsNullOrWhiteSpace(userId))
            return Response<AppointmentDto>.FailureResponse("User ID is required.");

        try
        {
            var patientId = await _unitOfWork.Repository<Patient>()
                .Query()
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (patientId == 0)
                return Response<AppointmentDto>.FailureResponse("Patient profile not found.");

            var appointment = await _unitOfWork.Repository<Appointment>()
                .Query()
                .Include(b => b.Schedule)
                .Include(b => b.Doctor).ThenInclude(d => d.User)
                .Include(b => b.Patient).ThenInclude(p => p.User)
                .Where(b => b.Id == appointmentId && !b.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (appointment is null)
                return Response<AppointmentDto>.FailureResponse("Appointment not found.");

            if (appointment.PatientId != patientId)
                return Response<AppointmentDto>.FailureResponse("You are not authorized to view this appointment.");

            var dto = new AppointmentDto
            {
                Id = appointment.Id,
                PatientName = appointment.Patient.User.FullName,
                DoctorName = appointment.Doctor.User.FullName,
                Date = appointment.Schedule.Day,
                StartTime = appointment.Schedule.StartTime,
                EndTime = appointment.Schedule.EndTime,
                Status = appointment.Status,
                ConsultationFee = appointment.Doctor.ConsultationFee,
                IsPaid = appointment.IsPaid
            };

            return Response<AppointmentDto>.SuccessResponse(dto);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetAppointmentByIdAsync cancelled. AppointmentId={Id}", appointmentId);
            return Response<AppointmentDto>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointment {AppointmentId}", appointmentId);
            return Response<AppointmentDto>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CANCEL APPOINTMENT
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<Response<bool>> CancelAppointmentAsync(
        int appointmentId, string userId, string reason, CancellationToken cancellationToken = default)
    {
        if (appointmentId <= 0)
            return Response<bool>.FailureResponse("Invalid appointment ID.");

        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 10)
            return Response<bool>.FailureResponse("A cancellation reason of at least 10 characters is required.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var patientId = await _unitOfWork.Repository<Patient>()
                .Query()
                .Where(p => p.UserId == userId)
                .Select(p => p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (patientId == 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("Patient profile not found.");
            }

            var appointment = await _unitOfWork.Repository<Appointment>()
                .Query()
                .Include(a => a.Schedule)
                .Where(a => a.Id == appointmentId && !a.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (appointment is null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("Appointment not found.");
            }

            if (appointment.PatientId != patientId)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("You are not authorized to cancel this appointment.");
            }

            if (appointment.Status == AppointmentStatus.Canceled)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("This appointment is already cancelled.");
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("Completed appointments cannot be cancelled.");
            }

            if (appointment.Schedule.StartTime <= DateTime.UtcNow.AddHours(2))
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("Appointments can only be cancelled at least 2 hours in advance.");
            }

            appointment.Status = AppointmentStatus.Canceled;
            appointment.CancelledAt = DateTime.UtcNow;
            appointment.UpdatedAt = DateTime.UtcNow;
            appointment.UpdatedBy = userId;
            _unitOfWork.Repository<Appointment>().Update(appointment);

            var schedule = appointment.Schedule;
            schedule.ScheduleStatus = ScheduleStatus.Avaliable;
            schedule.UpdatedAt = DateTime.UtcNow;
            schedule.UpdatedBy = userId;
            _unitOfWork.Repository<Schedule>().Update(schedule);

            var cancellationRequest = new CancellationRequest
            {
                AppointmentId = appointment.Id,
                RequestedByUserId = userId,
                Reason = reason.Trim(),
                Status = CancellationRequestStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                ReviewedAt = DateTime.UtcNow,
                AdminNotes = "Auto-approved: patient-initiated cancellation within policy."
            };
            await _unitOfWork.Repository<CancellationRequest>().AddAsync(cancellationRequest, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Appointment {AppointmentId} cancelled by patient {UserId}. Reason: {Reason}",
                appointmentId, userId, reason);

            return Response<bool>.SuccessResponse(true, "Appointment cancelled successfully.");
        }
        catch (OperationCanceledException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogWarning("CancelAppointmentAsync cancelled. AppointmentId={Id}", appointmentId);
            return Response<bool>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", appointmentId);
            return Response<bool>.FailureResponse("An unexpected error occurred while cancelling the appointment.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET DOCTOR APPOINTMENTS
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<Response<PagedResult<AppointmentDto>>> GetDoctorAppointmentsAsync(
        string userId, PaginationParameters parameters, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<PagedResult<AppointmentDto>>.FailureResponse("User ID is required.");

        if (parameters is null || parameters.PageNumber < 1 || parameters.PageSize < 1)
            return Response<PagedResult<AppointmentDto>>.FailureResponse("Invalid pagination parameters.");

        try
        {
            var doctorId = await _unitOfWork.Repository<Doctor>()
                .Query()
                .Where(d => d.UserId == userId)
                .Select(d => d.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (doctorId == 0)
                return Response<PagedResult<AppointmentDto>>.FailureResponse("Doctor profile not found.");

            var query = _unitOfWork.Repository<Appointment>()
                .Query()
                .Include(b => b.Schedule)
                .Include(b => b.Doctor).ThenInclude(d => d.User)
                .Include(b => b.Patient).ThenInclude(p => p.User)
                .Where(b => b.DoctorId == doctorId && !b.IsDeleted)
                .OrderByDescending(b => b.Schedule.StartTime);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                .Take(parameters.PageSize)
                .Select(b => new AppointmentDto
                {
                    Id = b.Id,
                    PatientName = b.Patient.User.FullName,
                    DoctorName = b.Doctor.User.FullName,
                    Date = b.Schedule.Day,
                    StartTime = b.Schedule.StartTime,
                    EndTime = b.Schedule.EndTime,
                    Status = b.Status,
                    ConsultationFee = b.Doctor.ConsultationFee,
                    IsPaid = b.IsPaid,
                    AppointmentsCount = totalCount
                })
                .ToListAsync(cancellationToken);

            return Response<PagedResult<AppointmentDto>>.SuccessResponse(new PagedResult<AppointmentDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetDoctorAppointmentsAsync cancelled. UserId={UserId}", userId);
            return Response<PagedResult<AppointmentDto>>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching doctor appointments for userId={UserId}", userId);
            return Response<PagedResult<AppointmentDto>>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET AVAILABLE TIME SLOTS FOR A DOCTOR ON A DATE
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<Response<List<TimeSlotDto>>> GetAvailableTimeSlotsAsync(
        int doctorId, DateTime date, CancellationToken cancellationToken = default)
    {
        if (doctorId <= 0)
            return Response<List<TimeSlotDto>>.FailureResponse("Invalid doctor ID.");

        if (date.Date < DateTime.UtcNow.Date)
            return Response<List<TimeSlotDto>>.FailureResponse("Cannot query time slots for a past date.");

        try
        {
            var doctorExists = await _unitOfWork.Repository<Doctor>()
                .Query()
                .AnyAsync(d => d.Id == doctorId && !d.IsDeleted, cancellationToken);

            if (!doctorExists)
                return Response<List<TimeSlotDto>>.FailureResponse("Doctor not found.");

            // Fetch to memory first, then project — avoids trying to translate
            // the IsAvailable computed property inside SQL.
            var schedules = await _unitOfWork.Repository<Schedule>()
                .Query()
                .Include(s => s.Appointment)
                .Where(s => s.DoctorId == doctorId
                         && !s.IsDeleted
                         && s.Day.Date == date.Date
                         && s.ScheduleStatus == ScheduleStatus.Avaliable)
                .OrderBy(s => s.StartTime)
                .ToListAsync(cancellationToken);

            var slots = schedules.Select(s => new TimeSlotDto
            {
                ScheduleId = s.Id,
                Date = s.Day,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                IsAvailable = s.Appointment is null
                           || s.Appointment.Status == AppointmentStatus.Canceled
            }).ToList();

            return Response<List<TimeSlotDto>>.SuccessResponse(
                slots,
                slots.Count == 0 ? "No available time slots for the selected date." : "Success");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetAvailableTimeSlotsAsync cancelled. DoctorId={DoctorId}, Date={Date}", doctorId, date);
            return Response<List<TimeSlotDto>>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching time slots for doctorId={DoctorId}, date={Date}", doctorId, date);
            return Response<List<TimeSlotDto>>.FailureResponse("An unexpected error occurred. Please try again.");
        }
    }
}