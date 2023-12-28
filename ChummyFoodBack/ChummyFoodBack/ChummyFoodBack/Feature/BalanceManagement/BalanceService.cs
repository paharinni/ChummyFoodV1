using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.Payment;

public class BalanceService: IBalanceService {
    private readonly CommerceContext _commerceContext;

    public BalanceService(CommerceContext commerceContext)
    {
        _commerceContext = commerceContext;
    }
    
    public async Task<double> GetCurrentBalance(string email)
    {
        var targetIdentity = await _commerceContext.Identities
            .Include(identities => identities.Payments)
            .FirstAsync(identity => identity.Email ==  email);
        
        
        var balanceAmount = targetIdentity.Payments
            .Where(payment => payment.PaymentStatus is PaymentStatus.Confirmed
                              && payment.StoredPaymentType is StoredPaymentType.BalanceUpdate 
                                  or StoredPaymentType.ProductPaymentFromBalance)
            .Select(payment => new {amount = payment.StoredPaymentType switch
            {
                StoredPaymentType.ProductPaymentFromBalance => -payment.PaymentAmount,
                StoredPaymentType.BalanceUpdate => +payment.PaymentAmount,
                StoredPaymentType.ProductPaymentFromWallet => 0
            }}) 
            .Sum(payment => payment.amount);

        return balanceAmount;
    }
}