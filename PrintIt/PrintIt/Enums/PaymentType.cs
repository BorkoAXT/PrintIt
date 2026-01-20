namespace PrintIt.Enums
{
    /// <summary>
    /// Represents the supported payment types for transactions.
    /// </summary>
    public enum PaymentType
    {
        /// <summary>Stripe payment</summary>
        Stripe = 0,

        /// <summary>PayPal payment</summary>
        PayPal = 1,

        /// <summary>Bank Transfer payment</summary>
        BankTransfer = 2,

        /// <summary>Google Pay payment</summary>
        GooglePay = 3
    }
}
