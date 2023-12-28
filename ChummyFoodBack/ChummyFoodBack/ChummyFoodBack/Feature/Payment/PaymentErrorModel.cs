namespace ChummyFoodBack.Feature.Payment;

public class PaymentErrorModel
{
    public string Reason { get; set; }
    public IEnumerable<int> FailedIds { get; set; }
}
