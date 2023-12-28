using System.Security.Claims;
using ChummyFoodBack.Extensions;
using ChummyFoodBack.Feature;
using ChummyFoodBack.Feature.Payment;
using ChummyFoodBack.Feature.Payment.Interfaces;
using ChummyFoodBack.Feature.RetailManagement.Products;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using ChummyFoodBack.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FansEcommerseSite.Feature.Payment;


public class PaymentCreationModel
{
    public double Amount { get; set; }
}

public class ProductPurchaceIdWithAmount
{
    public int Amount { get; set; }

    public int ProductId { get; set; }
}

public class GoodsPaymentModel: PaymentCreationModel
{
    public IEnumerable<ProductPurchaceIdWithAmount> ProductsToPurchase { get; set; }

    public string? Voucher { get; set; }
}



[ApiController]
[Authorize]
[Produces("application/json")]
[Consumes("application/json")]
[Route("api/[controller]")]
public class PaymentController: ControllerBase
{
    private readonly CommerceContext _commerceContext;
    private readonly ProductsEndpointGenerator _productsEndpointGenerator;
    private readonly ILogger<PaymentController> _logger;
    private readonly IPaymentService _paymentService;

    public PaymentController(
        ILogger<PaymentController> logger,
        IPaymentService paymentService,
        CommerceContext commerceContext,
        ProductsEndpointGenerator productsEndpointGenerator)
    {
        _logger = logger;
        _paymentService = paymentService;
        _commerceContext = commerceContext;
        _productsEndpointGenerator = productsEndpointGenerator;
    }


    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PaymentResponseViewModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments()
    {
        var currentEmail = this.HttpContext.User.Claims.First(claim => claim.Type is ClaimTypes.Email).Value;
        var associatedIdentity = await _commerceContext.Identities
            .Include(identity => identity.RoleDao)
            .SingleOrDefaultAsync(identity => identity.Email == currentEmail);
        if (associatedIdentity is null)
        {
            _logger.LogCritical("Unable to find authenticated identity");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        var identityAgnosticPayments = this.LoadAllPayments();
        if (associatedIdentity.RoleDao.Name == "Admin")
        {
            //Means that not apply filtering to admin
        }
        else
        {
            identityAgnosticPayments =
                identityAgnosticPayments.Where(payment => payment.Identity.Email == associatedIdentity.Email);
        }
        var responseViewModels = MapPaymentDaoToViewModel(identityAgnosticPayments);
        return Ok(responseViewModels);
    }
    

    private IQueryable<PaymentDAO> LoadAllPayments()
    {
        var paymentsWithIds = _commerceContext.Payments
            .Include(payment => payment.Identity)
            .AsSplitQuery()
            .Include(payment => payment.ProductCostItems)
            .ThenInclude(costItems => costItems.Product);
        return paymentsWithIds;
    }

    
    
    public IEnumerable<PaymentResponseViewModel> MapPaymentDaoToViewModel(IEnumerable<PaymentDAO> payments)
    {
        var responseViewModels = payments
            .GroupBy(payment => payment.Id).Select((group) =>
        {
            var payment = group.First();//In assumption that each group have one and only one value

            IEnumerable<ProductPurchaseViewModel>? purchaseViewModels = null;
            if (payment.StoredPaymentType is not StoredPaymentType.BalanceUpdate 
                && payment.PaymentStatus is PaymentStatus.Confirmed)
            {
                purchaseViewModels =
                    payment.ProductCostItems.GroupBy(costItem => costItem.Product.Id).Select(
                        (productGroupCostItems) =>
                        {
                            var groupProduct = productGroupCostItems.First().Product;
                            return new ProductPurchaseViewModel
                            {
                                Product = new StrippedProduct
                                {
                                    ProductName = groupProduct.Name,
                                    ProductPrice = groupProduct.Price,
                                    ProductPhotoUrl = _productsEndpointGenerator
                                        .CreatePhotoUrlFromProductId(Url, groupProduct.Id),
                                },
                                AttachedItems =
                                    productGroupCostItems.Select(costItem => costItem.UserUnderstandableItem)
                            };
                        });
            }

            return new PaymentResponseViewModel
            {
                Id = payment.Id,
                UserEmail = payment.Identity.Email,
                PaymentStatus = payment.PaymentStatus,
                PaymentAmount = payment.PaymentAmount,
                PaymentType = payment.StoredPaymentType,
                PaymentUrl = payment.InvoiceUrl,
                DateOfCreation = payment.DateOfCreation,
                DateOfConfirmation = payment.DateOfResolove,
                PurchasedProducts = purchaseViewModels,
            };
        })
            .OrderByDescending(model => model.DateOfCreation)
            .ThenBy(model => model.DateOfConfirmation);
        return responseViewModels;
    }

    [HttpPost("finalize")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> FinalizePayment(FinalizePaymentModel paymentRequest)
    {
        await _paymentService.CompletePaymentsWithIds(new int[]
        {
            paymentRequest.PaymentId
        });
        return Ok();
    }
    
    
    [HttpPost("fromBalance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentErrorModel), StatusCodes.Status424FailedDependency)]
    public async Task<IActionResult> PayForGoodsFromBalance(GoodsPaymentModel goodsPaymentModel)
    {
        var purchaseModel = FromGoodsPurchase(goodsPaymentModel);
        var paymentResult = await _paymentService
                .ProductPurchasePaymentFromBalance(purchaseModel);
        if (paymentResult.IsSuccess) {
                return Ok();
        }
        return MapFromErrorModel(paymentResult.Error!);
        
    }

    [HttpPost("fromWallet")]
    [ProducesResponseType(typeof(PaymentSuccessModel),StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentErrorModel), StatusCodes.Status424FailedDependency)]
    public async Task<IActionResult> PayForGoodsFromWallet(GoodsPaymentModel goodsPaymentModel)
    {
        var purchaseModel = FromGoodsPurchase(goodsPaymentModel);
        var paymentResult = await _paymentService
            .ProductPurchasePaymentFromWallet(purchaseModel);
        
        if (paymentResult.IsSuccess) {
            return Ok(new PaymentSuccessModel
            {
                PaymentUrl = paymentResult.Result!.PaymentUrl
            });
        }
        return MapFromErrorModel(paymentResult.Error!);
        
    }

    [HttpPost("fromWalletAnonymous")]
    [ProducesResponseType(typeof(PaymentSuccessWithCredentialsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaymentErrorModel), StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    public async Task<IActionResult> PerformAnonymousPaymentForGoods(AnonymousPaymentModel goodsPaymentModel)
    {
        var result = await _paymentService.ProductPurchasePaymentFromWalletAnonymous(goodsPaymentModel);
        if (result.IsSuccess)
        {
            return Ok(result.Result!);
        }

        return BadRequest(new PaymentErrorModel
        {
            Reason = result.Error!.Reason,
            FailedIds = result.Error!.FailedIds
        });
    }
    
    
    private ProductPurchase FromGoodsPurchase(GoodsPaymentModel goodsPaymentModel)
    {
        var purchaseModel = new ProductPurchase
        {
            Email = this.HttpContext.GetEmail()!,
            Voucher = goodsPaymentModel.Voucher,
            ProductsToPurchase = goodsPaymentModel.ProductsToPurchase.Select(item => new ProductPurchaseItem
            {
                ProductId = item.ProductId,
                Amount = item.Amount
            }),
        };
        return purchaseModel;
    }
    
    private IActionResult MapFromErrorModel(PaymentErrorModel errorModel)
    {
        return new JsonResult(new PaymentErrorModel
        {
            Reason = errorModel.Reason,
            FailedIds = errorModel.FailedIds
        })
        {
            StatusCode = StatusCodes.Status424FailedDependency
        };
    }
   
    
    
}
