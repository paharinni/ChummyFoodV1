namespace ChummyFoodBack.Feature.Notification;

[Flags]
public enum PaymentNotificationType
{
    WithVoucher = 0x0001,
    FromBalance = 0x0010
}