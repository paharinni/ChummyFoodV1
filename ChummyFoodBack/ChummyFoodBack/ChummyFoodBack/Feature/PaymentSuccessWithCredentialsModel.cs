using ChummyFoodBack.Feature.Payment;

namespace ChummyFoodBack.Feature;

public class PaymentSuccessWithCredentialsModel: PaymentSuccessModel
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}