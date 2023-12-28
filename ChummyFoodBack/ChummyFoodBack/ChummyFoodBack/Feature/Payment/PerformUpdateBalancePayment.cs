using ChummyFoodBack.Persistance.DAO;

namespace ChummyFoodBack.Feature.Payment;

public class PerformUpdateBalancePayment
{
    public string? ChargeCode { get; set; }

    public string? ChargeUrl { get; set; }

    public double Amount { get; set; }

    public string Email { get; set; }

    public PaymentStatus RequestedPaymentStatus { get; set; }
}

