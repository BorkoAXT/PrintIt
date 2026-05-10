namespace Entities.Models
{
    /// <summary>
    /// Configuration options for integration with myPOS Web Checkout
    /// (hosted Visa / Mastercard payment gateway).
    /// </summary>
    public class MyPosOptions
    {
        /// <summary>
        /// Store ID (SID) assigned by myPOS.
        /// 
        /// Identifies your merchant store in the myPOS system and is included
        /// in every payment request.
        /// 
        /// Provided by myPOS in the merchant dashboard.
        /// </summary>
        public string Sid { get; set; } = "";

        /// <summary>
        /// myPOS wallet number that will receive the funds from successful payments.
        /// 
        /// Example format: "61938166610"
        /// 
        /// This value is required when creating checkout requests.
        /// </summary>
        public string WalletNumber { get; set; } = "";

        /// <summary>
        /// Index of the private key used to sign checkout requests.
        /// 
        /// myPOS allows multiple signing keys; this value tells the gateway
        /// which public key to use when validating the request.
        /// 
        /// Common values are "1", "2", etc.
        /// </summary>
        public string KeyIndex { get; set; } = "";

        /// <summary>
        /// Merchant private key used to cryptographically sign checkout requests.
        /// 
        /// This key is provided by myPOS and must be kept strictly confidential.
        /// It is typically stored as:
        /// - a Base64-encoded string, or
        /// - a PEM-formatted RSA private key
        /// 
        /// </summary>
        public string PrivateKey { get; set; } = "";

        /// <summary>
        /// myPOS Web Checkout endpoint URL.
        /// 
        /// This is the hosted payment page where customers are redirected
        /// to complete the card payment (Visa / Mastercard).
        /// 
        /// Example:
        /// https://www.mypos.com/vmp/checkout
        /// </summary>
        public string Endpoint { get; set; } = "";
    }
}
