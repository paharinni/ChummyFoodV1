namespace ChummyFoodBack.Feature.VoucherManagement;

public class UserActivationResponse
{
    public string UserEmail { get; set; }

    public DateTimeOffset ActivationDate { get; set; }
}