namespace Entities.Models
{
    /// <summary>
    /// Root configuration object for all payment-related settings.
    /// 
    /// This class is typically bound from configuration
    /// (e.g. appsettings.json → "Payments" section)
    /// and injected via IOptions&lt;PaymentsOptions&gt;.
    /// </summary>
    public class PaymentsOptions
    {
        /// <summary>
        /// Public base URL of your application.
        /// 
        /// Used to construct absolute callback URLs that are sent to
        /// payment providers (success, cancel, and notification endpoints).
        /// 
        /// Example:
        /// https://printit.bg
        /// 
        /// ⚠️ Must be accessible from the internet for payment notifications.
        /// </summary>
        public string BaseUrl { get; set; } = "";

        /// <summary>
        /// Default currency used for payments.
        /// 
        /// Must be a valid ISO 4217 currency code supported by the
        /// configured payment providers (e.g. "BGN", "EUR").
        /// 
        /// otherwise payments may be rejected.
        /// </summary>
        public string Currency { get; set; } = "EUR";

        /// <summary>
        /// Configuration options for ePay.bg payments.
        /// 
        /// Includes merchant identification (CIN/MIN), secret key,
        /// and gateway endpoint used for redirect-based payments
        /// and server-to-server notifications.
        /// </summary>
        public EpayOptions Epay { get; set; } = new EpayOptions();

        /// <summary>
        /// Configuration options for myPOS Web Checkout payments.
        /// 
        /// Includes store identification, wallet number, cryptographic
        /// signing keys, and hosted checkout endpoint used for
        /// Visa / Mastercard card payments.
        /// </summary>
        public MyPosOptions MyPos { get; set; } = new MyPosOptions();
    }
}
