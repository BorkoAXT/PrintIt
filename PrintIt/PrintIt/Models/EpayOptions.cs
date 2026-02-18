namespace PrintIt.Models
{
    /// <summary>
    /// Configuration options for integration with ePay.bg
    /// (Bulgarian online payment provider).
    /// </summary>
    public class EpayOptions
    {
        /// <summary>
        /// Merchant Identification Number (MIN / CIN) assigned by ePay.
        /// 
        /// This value uniquely identifies your merchant account in ePay
        /// and is required in every payment request.
        /// 
        /// Provided by ePay during merchant onboarding.
        /// Example: "1234567890"
        /// </summary>
        public string Cin { get; set; } = "";

        /// <summary>
        /// Secret key (merchant password) used to sign requests and verify
        /// notifications from ePay.
        /// 
        /// This secret is used to generate the CHECKSUM value:
        /// HMAC-SHA1(ENCODED, Secret)
        /// 
        /// </summary>
        public string Secret { get; set; } = "";

        /// <summary>
        /// Base URL of the ePay payment gateway.
        /// 
        /// For production environments, this is typically:
        /// https://www.epay.bg/
        /// 
        /// This endpoint is used as the form POST action when redirecting
        /// the customer to the hosted ePay payment page.
        /// </summary>
        public string Endpoint { get; set; } = "https://www.epay.bg/";
    }
}