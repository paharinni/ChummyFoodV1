namespace ChummyFoodBack.Feature.Payment;

public class AnonymousPaymentModel
{
    public string RequestedEmail { get; set; }

    public string? Voucher { get; set; }

    public int ProductId { get; set; }

    public int ProductAmount { get; set; }
}