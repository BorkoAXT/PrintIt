using Stripe;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentIntent> CreatePaymentIntentAsync(Guid userId, long amountInCents);
    }
}
