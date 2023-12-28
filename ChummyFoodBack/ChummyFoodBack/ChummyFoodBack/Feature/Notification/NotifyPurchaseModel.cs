namespace ChummyFoodBack.Feature.Notification;

public class NotifyPurchaseModel
{
    public string CustomerEmail { get; set; }
    
    public IEnumerable<ProductPurchaseNotificationModel> ProductPurchaseNotificationModels { get; set; }
}
