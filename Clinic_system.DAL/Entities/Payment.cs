

namespace Clinic_System;

    public class Payment : BaseEntity
    {
    public int AppointmentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PaymentMethod { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? CheckoutSessionId { get; set; }
    public PaymentStatus? Status { get; set; }
    public string? TransactionReference { get; set; }
    public DateTime? PaidAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;


}
