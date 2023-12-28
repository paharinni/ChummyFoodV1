namespace ChummyFoodBack.Feature.Payment;


public class ProductNotificationItem
{
    public string ProductName { get; set; }

    public double ProductPriceForItem { get; set; }

    public IEnumerable<string> GoodsPayload { get; set; }
}

public class GoodsNotificationModel
{
    public string TargetEmail { get; set; }

    public IEnumerable<ProductNotificationItem> NotificationItems { get; set; }
 }