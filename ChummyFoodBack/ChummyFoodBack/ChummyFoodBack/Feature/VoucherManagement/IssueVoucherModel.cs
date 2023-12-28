namespace ChummyFoodBack.Feature.VoucherManagement;

public class IssueVoucherModel
{
    public double Discount { get; set; }

    public string Description { get; set; }

    public string Currency { get; set;}
    
    public int Days { get; set; }
}