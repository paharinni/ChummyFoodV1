namespace ChummyFoodBack.Feature.VoucherManagement;

public class VoucherCreationModel
{
    public string Email { get; set; }

    public string Currency { get; set; }
    
    public string Description { get; set; }

    public double Discount { get; set; }

    public int DaysRequested { get; set; }
    
    public DateTimeOffset IssueDate { get; set; }
}