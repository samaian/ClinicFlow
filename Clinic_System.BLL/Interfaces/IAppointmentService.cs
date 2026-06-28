namespace Clinic_System;

public interface IAppointmentService
{
    Task<Response<PagedResult<AppointmentDto>>> GetPatientAppointmensAsync(
        string userId, PaginationParameters parameters, CancellationToken cancellationToken = default);

    Task<Response<AppointmentDto>> GetAppointmentByIdAsync(
        int appointmentId, string userId, CancellationToken cancellationToken = default);

    Task<Response<bool>> CancelAppointmentAsync(
        int appointmentId, string userId, string reason, CancellationToken cancellationToken = default);

    Task<Response<PagedResult<AppointmentDto>>> GetDoctorAppointmentsAsync(
        string userId, PaginationParameters parameters, CancellationToken cancellationToken = default);

    Task<Response<List<TimeSlotDto>>> GetAvailableTimeSlotsAsync(
        int doctorId, DateTime date, CancellationToken cancellationToken = default);
}
