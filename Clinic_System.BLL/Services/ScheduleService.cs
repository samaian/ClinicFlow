using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class ScheduleService : IScheduleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(IUnitOfWork unitOfWork, ILogger<ScheduleService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response<List<ScheduleDto>>> GetDoctorSchedulesAsync(
        string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<List<ScheduleDto>>.FailureResponse("User ID is required.");

        try
        {
            var doctorId = await _unitOfWork.Repository<Doctor>()
                .Query().Where(d => d.UserId == userId).Select(d => d.Id)
                .FirstOrDefaultAsync(ct);

            if (doctorId == 0)
                return Response<List<ScheduleDto>>.FailureResponse("Doctor profile not found.");

            var schedules = await _unitOfWork.Repository<Schedule>()
                .Query()
                .Include(s => s.Appointment)
                .Where(s => s.DoctorId == doctorId && !s.IsDeleted)
                .OrderBy(s => s.Day).ThenBy(s => s.StartTime)
                .Select(s => new ScheduleDto
                {
                    Id             = s.Id,
                    Day            = s.Day,
                    StartTime      = s.StartTime,
                    EndTime        = s.EndTime,
                    Status         = s.ScheduleStatus,
                    HasAppointment = s.Appointment != null
                                  && s.Appointment.Status != AppointmentStatus.Canceled
                })
                .ToListAsync(ct);

            return Response<List<ScheduleDto>>.SuccessResponse(schedules);
        }
        catch (OperationCanceledException)
        {
            return Response<List<ScheduleDto>>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedules for userId={UserId}", userId);
            return Response<List<ScheduleDto>>.FailureResponse($"Error: {ex.Message}");
        }
    }

    public async Task<Response<bool>> CreateScheduleAsync(
        string userId, CreateScheduleDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        if (dto.StartTime >= dto.EndTime)
            return Response<bool>.FailureResponse("Start time must be before end time.");

        if (dto.Day.Date < DateTime.UtcNow.Date)
            return Response<bool>.FailureResponse("Cannot create a schedule slot in the past.");

        try
        {
            var doctorId = await _unitOfWork.Repository<Doctor>()
                .Query().Where(d => d.UserId == userId).Select(d => d.Id)
                .FirstOrDefaultAsync(ct);

            if (doctorId == 0)
                return Response<bool>.FailureResponse("Doctor profile not found.");

            // Prevent overlapping slots on the same day
            var overlaps = await _unitOfWork.Repository<Schedule>()
                .Query()
                .Where(s => s.DoctorId == doctorId
                         && !s.IsDeleted
                         && s.Day.Date == dto.Day.Date
                         && s.StartTime < dto.EndTime
                         && s.EndTime > dto.StartTime)
                .AnyAsync(ct);

            if (overlaps)
                return Response<bool>.FailureResponse("This time slot overlaps with an existing schedule.");

            var schedule = new Schedule
            {
                DoctorId       = doctorId,
                Day            = dto.Day.Date,
                StartTime      = dto.StartTime,
                EndTime        = dto.EndTime,
                ScheduleStatus = ScheduleStatus.Avaliable,
                CreatedBy      = userId
            };

            await _unitOfWork.Repository<Schedule>().AddAsync(schedule, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Response<bool>.SuccessResponse(true, "Schedule slot created successfully.");
        }
        catch (OperationCanceledException)
        {
            return Response<bool>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule for userId={UserId}", userId);
            return Response<bool>.FailureResponse($"Error: {ex.Message}");
        }
    }

    public async Task<Response<bool>> DeleteScheduleAsync(
        string userId, int scheduleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        if (scheduleId <= 0)
            return Response<bool>.FailureResponse("Invalid schedule ID.");

        try
        {
            var doctorId = await _unitOfWork.Repository<Doctor>()
                .Query().Where(d => d.UserId == userId).Select(d => d.Id)
                .FirstOrDefaultAsync(ct);

            var schedule = await _unitOfWork.Repository<Schedule>()
                .Query()
                .Include(s => s.Appointment)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && !s.IsDeleted, ct);

            if (schedule is null)
                return Response<bool>.FailureResponse("Schedule not found.");

            if (schedule.DoctorId != doctorId)
                return Response<bool>.FailureResponse("Not authorised to delete this schedule.");

            if (schedule.Appointment != null
                && schedule.Appointment.Status != AppointmentStatus.Canceled)
                return Response<bool>.FailureResponse(
                    "Cannot delete a slot that has an active appointment.");

            schedule.IsDeleted  = true;
            schedule.DeletedAt  = DateTime.UtcNow;
            schedule.DeletedBy  = userId;
            _unitOfWork.Repository<Schedule>().Update(schedule);
            await _unitOfWork.SaveChangesAsync(ct);

            return Response<bool>.SuccessResponse(true, "Schedule slot deleted.");
        }
        catch (OperationCanceledException)
        {
            return Response<bool>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule {ScheduleId}", scheduleId);
            return Response<bool>.FailureResponse($"Error: {ex.Message}");
        }
    }
}
