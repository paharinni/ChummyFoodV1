using ChummyFoodBack.Persistance.DAO;

namespace ChummyFoodBack.Feature.Payment;

public class StrippedProduct
{
    public string ProductName { get; set; }

    public double ProductPrice { get; set; }

    public string ProductPhotoUrl { get; set; }
}
public class ProductPurchaseViewModel
{
    public StrippedProduct Product { get; set; }
    public IEnumerable<string> AttachedItems { get; set; }
}
public class PaymentResponseViewModel
{
    public int Id { get; set; }

    public string UserEmail { get; set; }
    public DateTimeOffset DateOfCreation { get; set; }

    public DateTimeOffset? DateOfConfirmation { get; set; }

    public double PaymentAmount { get; set; }
    
    public PaymentStatus PaymentStatus { get; set; }

    public StoredPaymentType PaymentType { get; set; }

    public string? PaymentUrl { get; set; }

    public IEnumerable<ProductPurchaseViewModel>? PurchasedProducts { get; set; }
}