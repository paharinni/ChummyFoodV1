using System.Security.Cryptography;
using System.Text;
using ChummyFoodBack.Feature.VoucherManagement.Exceptions;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Shared;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Feature.VoucherManagement;

public class VoucherService : IVoucherService
{
    private readonly CommerceContext _commerceContext;

    public VoucherService(CommerceContext commerceContext)
    {
        _commerceContext = commerceContext;
    }


    public async Task<OperationValuedResult<VoucherCreationResult, string>> CreateVoucher(VoucherCreationModel model)
    {
        if (model.Currency is not ("%" or "$"))
        {
            return new()
            {
                IsSuccess = false,
                Error = "Currency could be either % or $"
            };
        }
        var voucherText = await CreateVoucherText(model.Email);
        var voucherDao = new VoucherDAO
        {
            Currency = model.Currency,
            Description = model.Description,
            Discount = model.Discount,
            Voucher = voucherText,
            IssueDate = model.IssueDate,
            ExpiryDate = model.IssueDate.AddDays(model.DaysRequested)
        };
        await _commerceContext.Vouchers.AddAsync(voucherDao);
        await _commerceContext.SaveChangesAsync();
        return new OperationValuedResult<VoucherCreationResult, string>
        {
            IsSuccess = true,
            Result = new VoucherCreationResult
            {
                Voucher = voucherText
            }
        };
    }
    private async Task<string> CreateVoucherText(string email)
    {
        var passwordHash = _commerceContext.Identities.FirstOrDefault(identity => identity.Email == email)?.PasswordHash;
        if (passwordHash is null)
        {
            throw new Exception($"Not found identity record for user email: {email}");
        }

        var hashPayload = Encoding.UTF8.GetBytes(email).Concat(Encoding.UTF8.GetBytes(passwordHash)).ToArray();
        var sha1 = new HMACSHA1();
        using var stream = new MemoryStream();
        await stream.WriteAsync(hashPayload, 0, hashPayload.Length);
        stream.Seek(0, SeekOrigin.Begin);
        var hash = await sha1.ComputeHashAsync(stream);
        var voucher = Convert.ToBase64String(hash).Substring(8);
        return voucher;
    }

    public async Task<OperationValuedResult<VoucherDAO, string>> VerifyVoucher(VoucherVerificationModel voucherVerification)
    {

        try
        {
            IdentityDAO targetIdentity = await ValidateIdentityByEmail(voucherVerification.UserEmail);

            VoucherDAO? voucherDao =
                await _commerceContext.Vouchers.SingleOrDefaultAsync(voucher =>
                    voucher.Voucher == voucherVerification.Voucher);
            if (voucherDao is null)
            {
                throw new VoucherValidationException("Coupon doesn't exists");
            }
            var dateDiff = voucherDao.ExpiryDate - DateTimeOffset.UtcNow;

            if (dateDiff.Milliseconds < 0)
            {
                throw new VoucherValidationException("Coupon is outdated");
            }
            bool isVoucherActivated = await _commerceContext.Payments
                .Where(payment => payment.IdentityId == targetIdentity.Id 
                                  && payment.VoucherActivationId != null)
                .Include(payment => payment.VoucherActivation)
                .AnyAsync(payment => payment.VoucherActivation!.VoucherId == voucherDao.Id);
            if (isVoucherActivated)
            {
                throw new VoucherValidationException("Coupon is already activated");
            }
            
            
            return new()
            {
                IsSuccess = true,
                Result = voucherDao
            };
        }
        catch (VoucherValidationException ex)
        {
            return new OperationValuedResult<VoucherDAO, string>
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }
    
    private async Task<IdentityDAO> ValidateIdentityByEmail(string userEmail)
    {
        
        IdentityDAO? targetIdentity = await _commerceContext.Identities
            .FirstOrDefaultAsync(identity =>
                identity.Email == userEmail);

        if (targetIdentity is null)
        {
            throw new VoucherValidationException("Identity wasn't found");
        }

        return targetIdentity;
    }
    
    public async Task<OperationValuedResult<VoucherApplicationResult, string>> ApplyVoucher(VoucherApplicationModel voucherApplicationModel)
    {
        var verificationResult = await this.VerifyVoucher(voucherApplicationModel);

        if (verificationResult.IsSuccess)
        {
            var resultVoucherModel = verificationResult.Result!;
            var requestedPrice = voucherApplicationModel.AmountBeforeApplication;
            
            var voucherApplyResultPrice=  resultVoucherModel.Currency switch
            {
                "$" => Math.Max(requestedPrice - resultVoucherModel.Discount, 0),
                "%" => (1 - resultVoucherModel.Discount / 100) * requestedPrice,
                _ => throw new InvalidOperationException($"Unable to apply discount on currency: {resultVoucherModel.Currency}")
            };
            return new OperationValuedResult<VoucherApplicationResult, string>()
            {
                IsSuccess = true,
                Result = new VoucherApplicationResult
                {
                    ResultAmount = Math.Round(voucherApplyResultPrice, 2),
                    VoucherUsed = resultVoucherModel
                }
            };
        }

        return new OperationValuedResult<VoucherApplicationResult, string>()
        {
            IsSuccess = false,
            Error = verificationResult.Error
        };
    }

    public async Task<OperationResult<string>> UpdateVoucherText(VoucherUpdateModel updateModel)
    {
        var voucherToUpdate = 
            await _commerceContext.Vouchers.FirstOrDefaultAsync(voucher => voucher.Id == updateModel.VoucherId);
        if (voucherToUpdate is null)
        {
            return new()
            {
                IsSuccess = false,
                Error = "Unable to found coupon with requested id"
            };
        }

        voucherToUpdate.Description = updateModel.VoucherDescription;
        await _commerceContext.SaveChangesAsync();
        return new() { IsSuccess = true };
    }

    public Task<bool> CheckVoucherExists(string voucher)
        => _commerceContext.Vouchers
            .AnyAsync(voucherDao => voucherDao.Voucher == voucher);
    

    public async Task<IEnumerable<VoucherResponseModel>> GetAllVouchers()
    {
        return await _commerceContext.Vouchers
            .Include(vouchers => vouchers.VoucherActivations)
            .ThenInclude(voucherActivation => voucherActivation.Payment)
            .ThenInclude(payment => payment.Identity)
            .Select(voucher => new VoucherResponseModel
            {
                Id = voucher.Id,
                Voucher = voucher.Voucher,
                Currency = voucher.Currency,
                Description = voucher.Description,
                Discount = voucher.Discount,
                IssueDate = voucher.IssueDate,
                TillDate = voucher.ExpiryDate,
                UserActivations = voucher.VoucherActivations.Select((activationDao) => new UserActivationResponse
                {
                    UserEmail = activationDao.Payment.Identity.Email,
                    ActivationDate = activationDao.ActivationDate
                })
            }).ToListAsync();
    }

    public async Task<OperationResult<string>> DeleteVoucher(int id)
    {
        var voucherToDelete = await _commerceContext.Vouchers
            .FirstOrDefaultAsync(voucher => voucher.Id == id);
        if (voucherToDelete is null)
        {
            return new OperationResult<string>
            {
                Error = "Unable to find coupon with requested id",
                IsSuccess = false
            };
        }

        _commerceContext.Vouchers.Remove(voucherToDelete);
        await _commerceContext.SaveChangesAsync();
        return new OperationResult<string>()
        {
            IsSuccess = true
        };
    }
}