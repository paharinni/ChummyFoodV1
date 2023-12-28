namespace ChummyFoodBack.Feature.Payment;

public class PaymentNotificationModel
{
    public required string UserEmail { get; set; }

    public required string ChargeLink { get; set; }

    public string MailSubject { get; set; }
}
