using ChummyFoodBack.Shared;

namespace ChummyFoodBack.Feature.Payment.Interfaces;


public class PaymentRequestModel
{
    public string Email { get; set; }

    public PaymentAmount Amount { get; set; }
}

public class ProductPurchaseItem
{
    public int ProductId { get; set; }

    public int Amount { get; set; }
}
 
public class ProductPurchase
{
    public string? Voucher { get; set; }
    public string Email { get; set; }
    public IEnumerable<ProductPurchaseItem> ProductsToPurchase { get; set; } 
}


public interface IPaymentService
{

    public Task<OperationValuedResult<PaymentSuccessWithCredentialsModel, PaymentErrorModel>> 
        ProductPurchasePaymentFromWalletAnonymous(AnonymousPaymentModel anonymousPaymentModel);
    public Task<OperationValuedResult<PaymentSuccessModel, string>> BalanceUpdatePayment(BalanceUpdateRequestModel paymentRequestModel);

    public Task<OperationValuedResult<PaymentSuccessModel, PaymentErrorModel>> ProductPurchasePaymentFromWallet(
        ProductPurchase productPurchaseModel);

    public Task<OperationResult<PaymentErrorModel>> ProductPurchasePaymentFromBalance(
        ProductPurchase productPurchaseModel);

    public Task RevertPaymentsWithIds(IEnumerable<int> paymentIds);

    public Task CompletePaymentsWithIds(IEnumerable<int> paymentIds);
}