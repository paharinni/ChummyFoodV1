using System.Security.Claims;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChummyFoodBack.Feature.VoucherManagement;

[ApiController]
[Produces("application/json")]
[Consumes("application/json")]
[Route("api/[controller]")]
[Authorize]
public class VoucherController : ControllerBase
{
    private readonly CommerceContext _commerce;
    private readonly IVoucherService _voucherService;
    
    public VoucherController(CommerceContext commerce, IVoucherService voucherService)
    {
        _commerce = commerce;
        _voucherService = voucherService;
    }
    
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<VoucherResponseModel>),StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVouchers()
    {
        var vouchers = await _voucherService.GetAllVouchers();
        return Ok(vouchers);
    }
    
    
    [HttpPost("Issue")]
    [ProducesResponseType(typeof(ErrorModel),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(VoucherModel),
        StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IssueVoucher(IssueVoucherModel model)
    {
        var userEmail = this.HttpContext.User.Claims.First(claim => claim.Type is ClaimTypes.Email).Value;
        var voucherCreationResult = await _voucherService.CreateVoucher(new VoucherCreationModel
        {
            Currency = model.Currency,
            Discount = model.Discount,
            Email = userEmail,
            IssueDate = DateTimeOffset.UtcNow,
            DaysRequested = model.Days,
            Description = model.Description
        });
    
        return Ok(new VoucherModel
        {
            Voucher = voucherCreationResult.Result!.Voucher
        });
    
    }
    
    [HttpPost("description")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateVoucherText(VoucherUpdateModel updateModel)
    {
        var targetVoucher = await _voucherService.UpdateVoucherText(updateModel);
        if (targetVoucher.IsSuccess)
        {
            return Ok();
        }
    
        return BadRequest(new ErrorModel
        {
            Errors = new[] { targetVoucher.Error! }
        });
    }
    
    
    [HttpPost("Check")]
    [ProducesResponseType(typeof(VoucherValidityModel),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyVoucher(VoucherModel model)
    {
        var userEmail = this.HttpContext.User
            .Claims.First(claim => claim.Type is ClaimTypes.Email)
            .Value;
        var verificationResult = await _voucherService.VerifyVoucher(new VoucherVerificationModel
        {
            Voucher = model.Voucher,
            UserEmail = userEmail
        });
    
        if (verificationResult.IsSuccess)
        {
            var voucherDao = _commerce.Vouchers.First(voucher => voucher.Voucher == model.Voucher);
            return Ok(new VoucherValidityModel 
            {
                IsValid = true,
                Currency = voucherDao.Currency,
                Discount = voucherDao.Discount,
                Reason = string.Empty
            });
        }
        return Ok(new VoucherValidityModel
        {
            IsValid = false,
            Reason = verificationResult.Error!
        });
    }
    
    
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteVoucher(int id)
    {
        var deleteVoucherOperationResult = await _voucherService.DeleteVoucher(id);
        if (deleteVoucherOperationResult.IsSuccess)
        {
            return Ok();
        }
    
        return BadRequest(new ErrorModel
        {
            Errors = new[] { deleteVoucherOperationResult.Error! }
        });
    }
}