using ChummyFoodBack.Feature.Payment.Interfaces;

namespace ChummyFoodBack.Feature.Payment;

public class PaymentResponse
{
    public string PaymentURL { get; set; }

    public string ChargeCode { get; set; }
    
}

public interface IPaymentAction
{ 
    public Task<PaymentResponse> CreateCharge(PaymentRequestModel payment, string checkoutName);
}
