namespace ChummyFoodBack.Feature.Payment;

public interface IBalanceService
{
    public Task<double> GetCurrentBalance(string userEmail);
}