using ChummyFoodBack.Feature.Payment;

namespace ChummyFoodBack.Feature.Notification;


public interface IUserNotificationService
{
    public Task NotifyUserCreated(UserModel userModel);

    Task NotifyUserRegistered(UserModel userModel);

    public Task NotifyRestorePassword(RestoreNotificationModel restoreNotificationModel);

    public Task NotifyPayment(PaymentNotificationModel model);

    public Task NotifyGoodsReceive(GoodsNotificationModel model);

    public Task NotifyAdminPurchase(NotifyPurchaseModel model, PaymentNotificationType type);
}
