namespace Clinic_System;

public interface IScheduleService
{
    Task<Response<List<ScheduleDto>>> GetDoctorSchedulesAsync(string userId, CancellationToken ct = default);
    Task<Response<bool>> CreateScheduleAsync(string userId, CreateScheduleDto dto, CancellationToken ct = default);
    Task<Response<bool>> DeleteScheduleAsync(string userId, int scheduleId, CancellationToken ct = default);
}
