#nullable disable
using Mentornote.Backend.Models;
using Mentornote.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System.Security.Claims;


namespace Mentornote.Backend.Controllers
{
    [ApiController]
    [Route("api/billing")]
    public class BillingController : Controller
    {
        private readonly DBServices _db;
        private readonly StripeSettings _stripe;

        public BillingController(DBServices db, StripeSettings stripe)
        {
            _db = db;
            _stripe = stripe;
        }

        // Desktop calls this -> open returned URL in system browser
        [Authorize]
        [HttpPost("checkout")]
        public IActionResult CreateCheckout()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                Mode = "subscription",
                SuccessUrl = _stripe.SuccessUrl,
                CancelUrl = _stripe.CancelUrl,
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = _stripe.PriceId,
                        Quantity = 1
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString()
                }
            };

            var service = new Stripe.Checkout.SessionService();
            Stripe.Checkout.Session session = service.Create(options);

            return Ok(new { url = session.Url });
        }

        // Desktop calls this -> open returned URL in system browser
        [Authorize]
        [HttpPost("portal")]
        public IActionResult CreateBillingPortal([FromBody] string stripeCustomerId)
        {
            // req.StripeCustomerId should come from YOUR DB (recommended),
            // but you can pass it if you already have it in memory.
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = stripeCustomerId,
                ReturnUrl = _stripe.ReturnUrl
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = service.Create(options);

            return Ok(new { url = session.Url });
        }

        // Stripe calls this. NO auth header. Verify signature.
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            string json = await new StreamReader(Request.Body).ReadToEndAsync();
            string signatureHeader = Request.Headers["Stripe-Signature"];

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, _stripe.WebhookSecret);
            }
            catch
            {
                return BadRequest();
            }

            // Payment succeeded -> activate subscription
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session?.Metadata == null || !session.Metadata.TryGetValue("userId", out var userIdStr))
                {
                    return Ok();
                }

                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Ok();
                }
                   

                // These are set for subscription-mode checkout
                var stripeCustomerId = session.CustomerId;
                var stripeSubscriptionId = session.SubscriptionId;

                if (!string.IsNullOrWhiteSpace(stripeCustomerId) && !string.IsNullOrWhiteSpace(stripeSubscriptionId))
                {
                    await _db.ActivateSubscriptionAsync(userId, stripeCustomerId, stripeSubscriptionId);
                }

                return Ok();
            }
            

            // Subscription canceled -> set IsSubscribed = false
            if (stripeEvent.Type == "customer.subscription.deleted")
            {
                var sub = stripeEvent.Data.Object as Stripe.Subscription;
                // If you store StripeSubscriptionId in Users, you can look up user by sub.Id.
                // MVP shortcut: you can also store mapping in another table.
                // Here’s the simplest approach: implement a SP that sets IsSubscribed=0 by StripeSubscriptionId.

                // Implement if you want:
                // await _db.SetSubscribedFalseByStripeSubscriptionIdAsync(sub.Id);

                return Ok();
            }

            // Payment failed -> optional (grace period logic later)
            if (stripeEvent.Type == "invoice.payment_failed")
            {
                return Ok();
            }

            return Ok();
        }

    }
}
