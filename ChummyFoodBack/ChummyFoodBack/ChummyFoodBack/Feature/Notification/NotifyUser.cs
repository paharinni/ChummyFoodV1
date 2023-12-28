using System.Text;
using ChummyFoodBack.Feature.Payment;
using ChummyFoodBack.Interactions;
using ChummyFoodBack.Interactions.Intefaces;
using ChummyFoodBack.Options;
using ChummyFoodBack.Persistance;
using Microsoft.Extensions.Options;

namespace ChummyFoodBack.Feature.Notification;

public class UserNotificationService : IUserNotificationService
{
    private readonly CommerceContext _commerceContext;
    private readonly IMailInteractionService _mailInteractionService;
    private readonly ILogger<UserNotificationService> _logger;
    private readonly IOptions<AdminUserOptions> _adminUserOptions;

    public UserNotificationService(
        IMailInteractionService mailInteractionService,
        ILogger<UserNotificationService> logger,
        IOptions<AdminUserOptions> adminUserOptions)
    {
        _mailInteractionService = mailInteractionService;
        _logger = logger;
        _adminUserOptions = adminUserOptions;
    }

    private string MakeFromNewLineInMail(string line)
        => $"<p>{line}</p>";

    public Task NotifyUserCreated(UserModel userModel)
        => HandleError(() => _mailInteractionService.SendMessageToSingleReceiver(new SendMailToOneReceiver
        {
            TargetRecipient = userModel.UserEmail,
            MailSubject = "[ChummyFood] Your registration details",
            Payload = "User credentials:\n\n" + $"Email: {userModel.UserEmail}\n\n" +
                      $"Password: {userModel.UserPassword}"
        }));

    public Task NotifyUserRegistered(UserModel userModel)
        => HandleError(() => _mailInteractionService.SendMessageToSingleReceiver(new SendMailToOneReceiver
        {
            TargetRecipient = userModel.UserEmail,
            MailSubject = "[ChummyFood] Your registration details",
            Payload = "User credentials:\n\n" + $"Email: {userModel.UserEmail}\n\n" +
                      $"Thank you for registration!"
        }));

    private string MakeBoldLineInMain(string text)
        => $"<b>{text}</b>";

    public Task NotifyRestorePassword(RestoreNotificationModel notificationModel)
        => HandleError(() => _mailInteractionService.SendMessageToSingleReceiver(
            new SendMailToOneReceiver
            {
                MailSubject = "[ChummyFood] Password restore",
                TargetRecipient = notificationModel.Email,
                Payload = new StringBuilder()
                    .AppendLine(MakeFromNewLineInMail("Code to restore your password: " 
                                                      + MakeBoldLineInMain(notificationModel.RestoreCode)))
                    .AppendLine(MakeFromNewLineInMail("Don't share your code with other users"))
                    .ToString()
            }));

    public Task NotifyAdminPurchase(NotifyPurchaseModel model, PaymentNotificationType type)
    {
        var payloadStringBuilder = new StringBuilder();
        payloadStringBuilder
            .AppendLine(MakeFromNewLineInMail($"Customer email: {model.CustomerEmail}"));
        foreach (var productLine in model.ProductPurchaseNotificationModels.Select(model =>
                     MakeFromNewLineInMail($"Product with name: {model.Name}. Purchased amount: {model.Amount}")))
        {
            payloadStringBuilder.AppendLine(productLine);
        }

        payloadStringBuilder.AppendLine(MakeFromNewLineInMail("Purchased products:"));
        var resultMailPayload = payloadStringBuilder.ToString();
        return HandleError(() =>
        {
            return _mailInteractionService.SendMessageToSingleReceiver(new SendMailToOneReceiver
            {
                MailSubject = type switch
                {
                    _ when type == (PaymentNotificationType.FromBalance | PaymentNotificationType.WithVoucher) => "[ChummyFood] balance purchase with voucher",
                    PaymentNotificationType.FromBalance => "[ChummyFood] balance purchase",
                    PaymentNotificationType.WithVoucher => "[ChummyFood] voucher purchase"
                },
                TargetRecipient = _adminUserOptions.Value.Email,
                Payload = resultMailPayload
            });
        });
    }

    public Task NotifyGoodsReceive(GoodsNotificationModel model)
    => HandleError(() =>
    {
        return _mailInteractionService.SendMessageToSingleReceiver(new SendMailToOneReceiver
        {
            MailSubject = "[ChummyFood] Your purchased items",
            TargetRecipient = model.TargetEmail,
            Payload = string.Join("<br/><br/>", model.NotificationItems.Select(notificationItem =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(MakeFromNewLineInMail("Product: " + notificationItem.ProductName));
                sb.AppendLine(MakeFromNewLineInMail("Product price: " + notificationItem.ProductPriceForItem + "$"));
                foreach (var goodPayload in notificationItem.GoodsPayload)
                {
                    sb.AppendLine(MakeFromNewLineInMail(goodPayload));
                }

                return sb.ToString();
            }))
        });
    });

    public Task NotifyPayment(PaymentNotificationModel paymentNotificationModel)
    => this.HandleError(() => _mailInteractionService.SendMessageToSingleReceiver(new SendMailToOneReceiver
    {
        TargetRecipient = paymentNotificationModel.UserEmail,
        Payload = $"Payment link: {paymentNotificationModel.ChargeLink}",
        MailSubject = paymentNotificationModel.MailSubject
    }));

    private async Task HandleError(Func<Task> result)
    {
        try
        {
            await result();
        }
        catch (Exception ex)
        {
            using var loggerScope = _logger.BeginScope("Failed notification");
            _logger.LogError(ex.Message, ex.StackTrace);
        }
    }
}
