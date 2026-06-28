using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Clinic_System;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IStripePaymentService stripePaymentService,
        IPaymentService paymentService,
        IConfiguration configuration,
        ILogger<PaymentsController> logger)
    {
        _stripePaymentService = stripePaymentService;
        _paymentService = paymentService;
        _configuration = configuration;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE CHECKOUT SESSION
    // ─────────────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Patient", AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    [HttpPost("checkout/{scheduleId:int}")]
    public async Task<IActionResult> CreateCheckoutSession(int scheduleId)
    {
        if (scheduleId <= 0)
            return BadRequest(new { error = "Invalid schedule ID." });

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { error = "User is not authenticated." });

        try
        {
            var successUrl = $"{Request.Scheme}://{Request.Host}/payment-success?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl  = $"{Request.Scheme}://{Request.Host}/payment-cancel";

            var result = await _stripePaymentService.CreateCheckoutSessionAsync(
                userId, scheduleId, successUrl, cancelUrl, HttpContext.RequestAborted);

            if (result.Success)
                return Ok(new { url = result.Data });

            return BadRequest(new { error = result.Message });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for scheduleId={ScheduleId}", scheduleId);
            return BadRequest(new { error = "Payment provider error. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating checkout session for scheduleId={ScheduleId}", scheduleId);
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STRIPE WEBHOOK
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        string json;
        try
        {
            json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read webhook body.");
            return BadRequest("Failed to read request body.");
        }

        var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();
        var endpointSecret  = _configuration["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(stripeSignature))
            return BadRequest("Missing Stripe-Signature header.");

        if (string.IsNullOrEmpty(endpointSecret))
        {
            _logger.LogError("Stripe WebhookSecret is not configured.");
            return StatusCode(500, "Webhook secret not configured.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, endpointSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
            return BadRequest($"Webhook signature verification failed: {ex.Message}");
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session == null)
            {
                _logger.LogWarning("checkout.session.completed event had no session object.");
                return Ok(); // Return 200 so Stripe doesn't retry
            }

            // ClientReferenceId encodes: "{userId}:{scheduleId}"
            // Build your Stripe session with ClientReferenceId = $"{userId}:{scheduleId}"
            var parts = session.ClientReferenceId?.Split(':');
            if (parts == null || parts.Length != 2
                || string.IsNullOrEmpty(parts[0])
                || !int.TryParse(parts[1], out int scheduleId))
            {
                _logger.LogWarning(
                    "Invalid ClientReferenceId in webhook: {ClientReferenceId}",
                    session.ClientReferenceId);
                return Ok(); // Not retryable – bad data
            }

            var userId  = parts[0];
            var amount  = (decimal)(session.AmountTotal ?? 0) / 100m;

            var result = await _paymentService.ConfirmPaymentAsync(
                userId, scheduleId,
                session.Id,
                session.PaymentIntentId ?? string.Empty,
                amount,
                HttpContext.RequestAborted);

            if (!result.Success)
            {
                _logger.LogError(
                    "ConfirmPaymentAsync failed. ScheduleId={ScheduleId}, Msg={Msg}",
                    scheduleId, result.Message);
                // Return 200 anyway so Stripe doesn't endlessly retry for business-logic failures
            }
        }

        return Ok();
    }
}
