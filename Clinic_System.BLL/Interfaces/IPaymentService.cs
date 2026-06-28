

namespace Clinic_System;

public interface IPaymentService
{
    Task<Response<bool>> ConfirmPaymentAsync(string userId, int scheduleId, string sessionId, string paymentIntentId, decimal amount, CancellationToken cancellationToken = default);
}
