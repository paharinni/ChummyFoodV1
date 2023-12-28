using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Shared;

namespace ChummyFoodBack.Feature.VoucherManagement;

public interface IVoucherService
{
    public Task<OperationValuedResult<VoucherCreationResult, string>> CreateVoucher(VoucherCreationModel creationModel);
    public Task<OperationValuedResult<VoucherDAO, string>> VerifyVoucher(VoucherVerificationModel voucher);
    public Task<OperationValuedResult<VoucherApplicationResult, string>> ApplyVoucher(VoucherApplicationModel applicationModel);
    public Task<OperationResult<string>> UpdateVoucherText(VoucherUpdateModel updateModel);

    public Task<bool> CheckVoucherExists(string voucher);
    public Task<IEnumerable<VoucherResponseModel>> GetAllVouchers();

    public Task<OperationResult<string>> DeleteVoucher(int id);
}