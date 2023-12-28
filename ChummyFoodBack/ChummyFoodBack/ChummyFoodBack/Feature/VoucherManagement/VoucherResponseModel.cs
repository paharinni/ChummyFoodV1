namespace ChummyFoodBack.Feature.VoucherManagement;

public class VoucherResponseModel
{
    public int Id { get; set; }

    public string Voucher { get; set; }

    public string Description { get; set; }

    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset TillDate { get; set; }

    public double Discount { get; set; }

    public IEnumerable<UserActivationResponse> UserActivations { get; set; }
    
    public string Currency { get; set; }
}