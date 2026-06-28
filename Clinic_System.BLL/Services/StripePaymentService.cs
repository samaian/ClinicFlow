

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace Clinic_System;

public class StripePaymentService : IStripePaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public StripePaymentService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<Response<string>> CreateCheckoutSessionAsync(string userId, int scheduleId, string successUrl, string cancelUrl, CancellationToken cancellationToken = default)
    {
        // 1. جلب الـ Schedule باستخدام Include
        var schedule = await _unitOfWork.Repository<Schedule>()
            .Query()
            .Include(s => s.Doctor).ThenInclude(d => d!.User)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, cancellationToken);

        if (schedule == null)
            return Response<string>.FailureResponse("Schedule not found.");

        // 2. التحقق من عدم وجود حجز مدفوع مسبقاً (مع تجاهل الملغاة)
        var existingAppointment = await _unitOfWork.Repository<Appointment>()
            .Query()
            .FirstOrDefaultAsync(a => a.ScheduleId == scheduleId && a.Status != AppointmentStatus.Canceled , cancellationToken);

        if (existingAppointment != null)
            return Response<string>.FailureResponse("Schedule already booked .");

        // 3. إنشاء جلسة Stripe
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)Math.Round(schedule.Doctor!.ConsultationFee * 100, MidpointRounding.AwayFromZero),
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Consultation with Dr. {schedule.Doctor.User.FullName}",
                    },
                },
                Quantity = 1,
            },
        },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = scheduleId.ToString(),
            Metadata = new Dictionary<string, string>
           {
            { "ScheduleId", scheduleId.ToString() },
            { "UserId", userId }
              }
        };

        var service = new SessionService();
        Session session;
        try
        {
            session = await service.CreateAsync(options, cancellationToken: cancellationToken);
        }
        catch (StripeException stripeEx)
        {
            return Response<string>.FailureResponse($"Stripe error: {stripeEx.Message}");
        }

        // 4. بعد نجاح Stripe، احصل على PatientId
        var patientId = await _unitOfWork.Repository<Patient>()
            .Query()
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (patientId == 0)
            return Response<string>.FailureResponse("Patient profile not found.");

        // 5. أنشئ الـ Appointment باستخدام ScheduleId فقط (لا تستخدم schedule نفسه)
      

        // 7. أرجع رابط الدفع
        return Response<string>.SuccessResponse(session.Url, "Checkout session created.");
    }
}