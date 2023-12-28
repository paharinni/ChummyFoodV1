using ChummyFoodBack.Persistance.DAO;

namespace ChummyFoodBack.Feature.VoucherManagement;

public class VoucherApplicationResult
{
    public double ResultAmount { get; set; }

    public VoucherDAO VoucherUsed { get; set; }
}