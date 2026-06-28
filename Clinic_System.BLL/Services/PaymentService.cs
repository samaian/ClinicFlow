using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clinic_System;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<PaymentService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _emailTemplateService = emailTemplateService ?? throw new ArgumentNullException(nameof(emailTemplateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Response<bool>> ConfirmPaymentAsync(
        string userId,
        int scheduleId,
        string sessionId,
        string paymentIntentId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        // ── Input validation (no transaction needed yet) ──────────────────────
        if (string.IsNullOrWhiteSpace(userId))
            return Response<bool>.FailureResponse("User ID is required.");

        if (scheduleId <= 0)
            return Response<bool>.FailureResponse("Invalid schedule ID.");

        if (string.IsNullOrWhiteSpace(sessionId))
            return Response<bool>.FailureResponse("Session ID is required.");

        if (amount <= 0)
            return Response<bool>.FailureResponse("Payment amount must be greater than zero.");

        // ── Idempotency check — read-only, before opening a transaction ───────
        if (!string.IsNullOrWhiteSpace(paymentIntentId))
        {
            var existingPayment = await _unitOfWork.Repository<Payment>()
                .Query()
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, cancellationToken);

            if (existingPayment is not null)
            {
                _logger.LogInformation("Duplicate payment intent {PaymentIntentId} – skipping.", paymentIntentId);
                return Response<bool>.SuccessResponse(true, "Payment already processed.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Load and validate schedule
            var schedule = await _unitOfWork.Repository<Schedule>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == scheduleId && !s.IsDeleted, cancellationToken);

            if (schedule is null)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("Schedule not found.");
            }

            if (schedule.ScheduleStatus == ScheduleStatus.NotAvaliable)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Response<bool>.FailureResponse("This time slot has already been booked. Please choose another.");
            }

            // 2. Resolve patient
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

            // 3. Create appointment
            var appointment = new Appointment
            {
                ScheduleId = scheduleId,
                PatientId = patientId,
                DoctorId = schedule.DoctorId,
                Status = AppointmentStatus.Confirmed,
                IsPaid = true,
                CreatedBy = userId
            };
            await _unitOfWork.Repository<Appointment>().AddAsync(appointment, cancellationToken);

            // 4. Mark schedule as taken
            schedule.ScheduleStatus = ScheduleStatus.NotAvaliable;
            schedule.UpdatedAt = DateTime.UtcNow;
            schedule.UpdatedBy = userId;
            _unitOfWork.Repository<Schedule>().Update(schedule);

            // 5. Record payment — EF resolves FK order automatically in one flush,
            //    so we don't need a separate SaveChangesAsync just to get appointment.Id.
            var payment = new Payment
            {
                Appointment = appointment,   // navigation ref, not Id — EF sets the FK after insert
                CheckoutSessionId = sessionId,
                PaymentIntentId = paymentIntentId,
                Amount = amount,
                Status = PaymentStatus.Completed,
                PaidAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Payment>().AddAsync(payment, cancellationToken);

            // 6. Single flush + commit
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Payment confirmed. UserId={UserId}, ScheduleId={ScheduleId}, Amount={Amount}",
                userId, scheduleId, amount);

            // 7. Best-effort confirmation email — always use CancellationToken.None
            //    so a cancelled caller request doesn't abort the email fire-and-forget.
            _ = SendPaymentConfirmationEmailAsync(appointment, amount);

            return Response<bool>.SuccessResponse(true, "Payment confirmed successfully.");
        }
        catch (OperationCanceledException)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogWarning("ConfirmPaymentAsync cancelled. ScheduleId={ScheduleId}", scheduleId);
            return Response<bool>.FailureResponse("Request was cancelled.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            _logger.LogError(ex, "Error confirming payment for scheduleId={ScheduleId}", scheduleId);
            return Response<bool>.FailureResponse("An unexpected error occurred while processing the payment. Please contact support.");
        }
    }

    // ── Private helper ────────────────────────────────────────────────────────

    // No CancellationToken parameter — this always runs to completion regardless
    // of the original request's cancellation state.
    private async Task SendPaymentConfirmationEmailAsync(Appointment appointment, decimal amount)
    {
        try
        {
            var fullAppointment = await _unitOfWork.Repository<Appointment>()
                .Query()
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User)
                .Include(a => a.Schedule)
                .FirstOrDefaultAsync(a => a.Id == appointment.Id);

            if (fullAppointment?.Patient?.User?.Email is null)
            {
                _logger.LogWarning("Cannot send payment email: patient email missing for appointmentId={Id}", appointment.Id);
                return;
            }

            var template = await _emailTemplateService.GetTemplateAsync("PaymentConfirmation.html");
            if (string.IsNullOrWhiteSpace(template))
            {
                _logger.LogWarning("Payment confirmation email template not found.");
                return;
            }

            var body = template
                .Replace("{{User}}", fullAppointment.Patient.User.FullName)
                .Replace("{{DoctorName}}", fullAppointment.Doctor?.User?.FullName ?? "your doctor")
                .Replace("{{AppointmentDate}}", fullAppointment.Schedule?.StartTime.ToString("f") ?? "—")
                .Replace("{{Amount}}", amount.ToString("N2"));

            await _emailService.SendEmailAsync(
                fullAppointment.Patient.User.Email,
                "Payment Confirmation",
                body,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation email for appointmentId={Id}", appointment.Id);
        }
    }
}