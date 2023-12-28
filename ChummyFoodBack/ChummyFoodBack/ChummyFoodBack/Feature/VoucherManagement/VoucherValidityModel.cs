namespace ChummyFoodBack.Feature.VoucherManagement;

public class VoucherValidityModel
{
    public string Reason { get; set; }
    
    public bool IsValid { get; set; }

    public double? Discount { get; set; }

    public string? Currency { get; set; }
}