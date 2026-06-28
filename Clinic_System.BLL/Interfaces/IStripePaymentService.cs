
namespace Clinic_System;

public interface IStripePaymentService
{
    Task<Response<string>> CreateCheckoutSessionAsync(string userId, int scheduleId, string successUrl, string cancelUrl, CancellationToken cancellationToken = default);
}
