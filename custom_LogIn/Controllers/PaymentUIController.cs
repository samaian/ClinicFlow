using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe.V2;

namespace Clinic_System;

public class PaymentUIController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentUIController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("/payment-success")]
        public async Task<IActionResult> Success(string session_id)
        {
        if (string.IsNullOrEmpty(session_id))
            return BadRequest("Session ID is missing.");
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(session_id);

        if (session.PaymentStatus != "paid")
            return BadRequest("Payment not completed.");

        // لو حاطط Metadata وقت إنشاء الـ Session
        var scheduleId = int.Parse(session.Metadata["ScheduleId"]);
        var amount = (decimal)(session.AmountTotal ?? 0) / 100m; // Convert from cents to dollars
       var result = await _paymentService.ConfirmPaymentAsync(userId!,scheduleId, session.Id, session.PaymentIntentId ?? string.Empty, amount, HttpContext.RequestAborted);
        if (result.Success)
            return View();

        else return BadRequest();
        }

        [HttpGet("/payment-cancel")]
        public IActionResult Cancel()
        {
            return View();
        }

    
}
