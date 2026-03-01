using Stripe;

namespace PrintIt.Services
{
    public interface IPaymentService
    {
        Task<PaymentIntent> CreatePaymentIntentAsync(Guid userId, long amountInCents);
    }
}
