using System.Security.Claims;
using ChummyFoodBack.Extensions;
using ChummyFoodBack.Feature.Payment;
using ChummyFoodBack.Feature.Payment.Interfaces;
using ChummyFoodBack.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Security;

namespace FansEcommerseSite.Feature.Payment;

[Produces("application/json")]
[ApiController]
[Consumes("application/json")]
[Route("api/[controller]")]
[Authorize]
public class BalanceController: ControllerBase
{
    private readonly IBalanceService _balanceService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<BalanceController> _logger;

    public BalanceController(
        IBalanceService balanceService, 
        IPaymentService paymentService,
        ILogger<BalanceController> logger)
    {
        _balanceService = balanceService;
        _paymentService = paymentService;
        _logger = logger;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(BalanceStatusModel),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> ReadBalance()
    {
        return Ok(0);
        // Todo: uncomment
        // var userEmail
        //     = this.HttpContext.User.Claims.First(claim => claim.Type is ClaimTypes.Email)
        //         .Value;
        // var balance = await _balanceService.GetCurrentBalance(userEmail);
        // return Ok(new BalanceStatusModel
        // {
        //     Balance = balance
        // });
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status424FailedDependency)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBalance(PaymentCreationModel paymentCreationModel)
    {
        if (paymentCreationModel.Amount <= 0)
        {
            return BadRequest(new ErrorModel
            {
                Errors = new[]{"Amount should be positive value"}
            });
        }

        return await BranchEmailLogic(nameof(UpdateBalance), async (email) =>
        { 
            var paymentResult = await _paymentService.BalanceUpdatePayment(new BalanceUpdateRequestModel
            {
                Email = email,
                Amount = paymentCreationModel.Amount
            });
            if (!paymentResult.IsSuccess)
            {
                return new JsonResult(new ErrorModel
                {
                    Errors = new[] { paymentResult.Error! }
                })
                {
                    StatusCode = StatusCodes.Status424FailedDependency
                };
            }
            return Ok(new PaymentResponseModel
            {
                PaymentUrl = paymentResult.Result!.PaymentUrl
            });
        });
    }


    private Task<IActionResult> BranchEmailLogic(string actionName, Func<string, Task<IActionResult>> retrievalLogic)
    {
        var userEmail = this.HttpContext.GetEmail();
        if( userEmail is null){
            var endpointName = Url.Action(actionName, "Payment");
            if (endpointName is null)
            {
                throw new InvalidParameterException (
                    "Should be passed valid payment controller action on property: " + nameof(actionName));
            }
            _logger.LogCritical($"Requested claim identity from endpoint {endpointName}");
            return Task.FromResult(new StatusCodeResult(StatusCodes.Status500InternalServerError) as IActionResult);
        }

        return retrievalLogic(userEmail);
    }


}