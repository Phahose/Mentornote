namespace Mentornote.Backend.Models
{
    public class StripeSettings
    {
        public string SecretKey { get; set; }
        public string WebhookSecret { get; set; }
        public string PriceId { get; set; }
        public string SuccessUrl { get; set; }
        public string CancelUrl { get; set; }
        public string ReturnUrl { get; set; }
    }
}
