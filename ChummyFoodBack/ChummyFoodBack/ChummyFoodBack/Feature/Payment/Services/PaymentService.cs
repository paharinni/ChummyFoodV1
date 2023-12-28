using System.Transactions;
using ChummyFoodBack.Feature.Notification;
using ChummyFoodBack.Feature.Payment;
using ChummyFoodBack.Feature.Payment.Interfaces;
using ChummyFoodBack.Persistance;
using ChummyFoodBack.Persistance.DAO;
using Microsoft.EntityFrameworkCore;
using ChummyFoodBack.Feature.IdentityManagement;
using ChummyFoodBack.Shared;
using ChummyFoodBack.Feature;
using ChummyFoodBack.Feature.VoucherManagement;

namespace FansEcomerseSite.Feature.Payment.Services;

public class PaymentService: IPaymentService
{
    private readonly double Precision = 0.00001;
    private readonly IIdentityService _identityService;
    private readonly CommerceContext _commerceContext;
    private readonly IVoucherService _voucherService;
    private readonly IPaymentAction _issueCoinbasePayment;
    private readonly ILogger<PaymentService> _logger;
    private readonly IUserNotificationService _notificationService;
    private const string PurchaseChargeLabel = "Product payment";
    public PaymentService(
        IIdentityService identityService,
        CommerceContext commerceContext,
        IVoucherService voucherService,
        IPaymentAction issueCoinbasePayment,
        ILogger<PaymentService> logger,
        IUserNotificationService notificationService)
    {
        _identityService = identityService;
        _commerceContext = commerceContext;
        _voucherService = voucherService;
        _issueCoinbasePayment = issueCoinbasePayment;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<OperationValuedResult<PaymentSuccessWithCredentialsModel, PaymentErrorModel>> ProductPurchasePaymentFromWalletAnonymous(
        AnonymousPaymentModel anonymousPaymentModel)
    {
        string email = anonymousPaymentModel.RequestedEmail;
        if (!(await _commerceContext.Identities.AnyAsync(identity => identity.Email == email)))
        {
            var registrationResult = await _identityService
                .RegisterUserWithoutPassword(email);
            if (registrationResult.IsSuccess)
            {
                return await this.PerformPayment(anonymousPaymentModel.RequestedEmail, registrationResult.Result!.Password, anonymousPaymentModel);
            }
            return new()
            {
                IsSuccess = false,
                Error = new PaymentErrorModel
                {
                    Reason = registrationResult.Error!,
                    FailedIds = Enumerable.Empty<int>(),
                }
            };
        }
        
        return await this.PerformPayment(null, null, anonymousPaymentModel);
    }

    private async Task<OperationValuedResult<PaymentSuccessWithCredentialsModel, PaymentErrorModel>> PerformPayment(
        string? responseEmail, 
        string? responsePassword, 
        AnonymousPaymentModel model)
    {
        var paymentResult = await this.ProductPurchasePaymentFromWallet(new ProductPurchase
        {
            Email = model.RequestedEmail,
            Voucher = model.Voucher,
            ProductsToPurchase = new[]{new ProductPurchaseItem
            {
                Amount = model.ProductAmount,
                ProductId = model.ProductId,
            }},
        });
        if (!paymentResult.IsSuccess)
        {
            return new()
            {
                Error = paymentResult.Error,
                IsSuccess = false
            };
        } 
        return new()
        {
            IsSuccess = true,
            Result = new PaymentSuccessWithCredentialsModel
            {
                Email = responseEmail,
                PaymentUrl = paymentResult.Result!.PaymentUrl,
                Password = responsePassword,
            },
        };
    }

    public async Task<OperationValuedResult<PaymentSuccessModel, string>> BalanceUpdatePayment(BalanceUpdateRequestModel paymentRequestModel)
    {
        try
        {
            var chargeCreationModel =
                await _issueCoinbasePayment.CreateCharge(new PaymentRequestModel
                {
                    Email = paymentRequestModel.Email,
                    Amount = new PaymentAmount(null, paymentRequestModel.Amount)
                }, "Top up you balance");


            this.UpdateBalance(new PerformUpdateBalancePayment
            {
                Amount = paymentRequestModel.Amount,
                Email = paymentRequestModel.Email,
                ChargeCode = chargeCreationModel.ChargeCode,
                ChargeUrl = chargeCreationModel.PaymentURL,
                RequestedPaymentStatus = PaymentStatus.WaitForPayment
            });
            
            await _notificationService.NotifyPayment(new PaymentNotificationModel
            {
                ChargeLink = chargeCreationModel.PaymentURL,
                UserEmail = paymentRequestModel.Email,
                MailSubject = "[ChummyFood] Your payment link"
            });

            
            await _commerceContext.SaveChangesAsync();
            return new OperationValuedResult<PaymentSuccessModel, string>()
            {
                IsSuccess = true,
                Result = new PaymentSuccessModel
                {
                    PaymentUrl = chargeCreationModel.PaymentURL
                }
            };
        }
        catch (PaymentException paymentException)
        {
            _logger.LogError(paymentException.Message, paymentException.StackTrace);
            return new OperationValuedResult<PaymentSuccessModel, string>
            {
                Error = paymentException.Message,
                IsSuccess = false
            };
        }
    }

   
    private void UpdateBalance(PerformUpdateBalancePayment performBalanceUpdate)
    {
        var userIdentity = _commerceContext.Identities
            .First(identity => identity.Email == performBalanceUpdate.Email);
            
        _commerceContext.Payments.Add(new PaymentDAO
        {
            StoredPaymentType = StoredPaymentType.BalanceUpdate,
            PaymentStatus = performBalanceUpdate.RequestedPaymentStatus,
            PaymentAmount = performBalanceUpdate.Amount,
            DateOfCreation = DateTimeOffset.UtcNow,
            InvoiceCode = performBalanceUpdate.ChargeCode,
            InvoiceUrl = performBalanceUpdate.ChargeUrl,
            DateOfResolove = performBalanceUpdate.RequestedPaymentStatus switch
            {
                PaymentStatus.WaitForPayment => null,
                PaymentStatus.Confirmed => DateTimeOffset.UtcNow,
                _ => throw new InvalidOperationException("Failed to update balance")
            },
            Identity = userIdentity
        });
        
    }


    public Task<OperationValuedResult<PaymentSuccessModel, PaymentErrorModel>> ProductPurchasePaymentFromWallet(ProductPurchase productPurchaseModel)
        => RunAfterAmountCalculated<OperationValuedResult<PaymentSuccessModel, PaymentErrorModel>>(productPurchaseModel,
            async (purchaseAmount, productIdsToAmount, requestedProducts) =>
            {
                try
                {
                    var totalAmountToBuy = purchaseAmount.TotalAmount;
                    //In range of -0.00001 <= amount <= 0.00001
                    if (totalAmountToBuy <= Precision && totalAmountToBuy >= -Precision)
                    {
                        
                        var targetIdentity = _commerceContext.Identities
                            .First(identity => productPurchaseModel.Email == identity.Email);
                        await PerformPaymentWithoutCrypto(targetIdentity, 
                            purchaseAmount, 
                            productIdsToAmount,
                            requestedProducts,
                            StoredPaymentType.ProductPaymentFromWallet );
                        return new()
                        {
                            IsSuccess = true,
                            Result = new PaymentSuccessModel
                            {
                                PaymentUrl = ""
                            }
                        };
                    }
                    string chargeUrl = await PerformPaymentAndReturnUrl(requestedProducts, productIdsToAmount,
                        new PaymentRequestModel
                        {
                            Amount = purchaseAmount,
                            Email = productPurchaseModel.Email
                        });
                    return new()
                    {
                        IsSuccess = true,
                        Result = new PaymentSuccessModel
                        {
                            PaymentUrl = chargeUrl
                        }
                    };
                }
                catch (PaymentException paymentException)
                {
                    _logger.LogError(paymentException.Message, paymentException.StackTrace);
                    return new()
                    {
                        IsSuccess = false,
                        Error = new PaymentErrorModel
                        {
                            Reason = "Failed to create invoice",
                            FailedIds = Enumerable.Empty<int>()
                        }
                    };
                }
            });
    


    private async Task<string> PerformPaymentAndReturnUrl(
        List<ProductDAO> requestedProducts,
        Dictionary<int, int> productIdToAmountRequested,
        PaymentRequestModel paymentModel)
    {
            //Charge created
            var charge = await _issueCoinbasePayment.CreateCharge(new PaymentRequestModel
            {
                Email = paymentModel.Email ,
                Amount = paymentModel.Amount
            }, PurchaseChargeLabel);
            
            //Identity fetched
            var targetIdentity = _commerceContext.Identities
                .First(identity => identity.Email == paymentModel.Email);
            
            //Payment created for matched user identity and reservation 
            var payment = new PaymentDAO
            {
                PaymentStatus = PaymentStatus.WaitForPayment,
                StoredPaymentType = StoredPaymentType.ProductPaymentFromWallet,
                Identity = targetIdentity,
                InvoiceCode = charge.ChargeCode,
                InvoiceUrl = charge.PaymentURL,
                PaymentAmount = paymentModel.Amount.TotalAmount,
                DateOfCreation = DateTimeOffset.UtcNow,
                DateOfResolove = null,
                RequestedProducts = requestedProducts.Select(productToBuy => new RequestedProductToBuyDAO
                {
                   Product = productToBuy,
                   ItemsCountRequested = productIdToAmountRequested[productToBuy.Id]
                }).ToList(),
                VoucherActivation = paymentModel.Amount.VoucherUsed is not null? new VoucherActivationDAO
                {
                    Voucher = paymentModel.Amount.VoucherUsed,
                    ActivationDate = DateTimeOffset.UtcNow,
                }: null,
            };
            
          
            _commerceContext.Payments.Add(payment);

            try
            {
                await _notificationService.NotifyPayment(new PaymentNotificationModel
                {
                    ChargeLink = charge.PaymentURL,
                    UserEmail = paymentModel.Email,
                    MailSubject = "[ChummyFood] Your payment link"
                });
            }
            catch (UserNotificationException ex)
            {
                //Here is not actually critical to guarantee delivery of message to user
                _logger.LogError(ex.Message, ex.StackTrace);
            }

            return charge.PaymentURL;
    }

    private async Task<(Dictionary<int, int> productIdToAmount, List<ProductDAO> persitedProducts)> FetchInitialProductInfo(
        IEnumerable<ProductPurchaseItem> productPurchaseItems)
    {
        var productIdsDict = productPurchaseItems
            .ToDictionary(product => product.ProductId, product => product.Amount);
        
        var ids = productIdsDict.Keys.ToList();
        var requestedProducts =
            await _commerceContext.Products
                .Include(product => product.ProductCostItems)
                .Where(product => ids.Contains(product.Id))
                .ToListAsync();
        return new(productIdsDict, requestedProducts);

    }

    private (double? totalRequestedCount, OperationResult<PaymentErrorModel>? result) PerformPaymentCalculationAndValidation(
        Dictionary<int, int> productIdsToAmount, 
        List<ProductDAO> requestedProducts)
    {
        var requestedIds = productIdsToAmount.Keys.ToList();
        if (requestedProducts.Count != requestedIds.Count)
        {
            return (null,  new ()
            {
                IsSuccess = false,
                Error = new PaymentErrorModel
                {
                    Reason = "Products with requested ids doesn't exists",
                    FailedIds = requestedIds.Except(requestedProducts.Select(product => product.Id))
                }
            });
        }
        

        double totalAmountToBuy = 0;
        List<int> failedProductValidation = new List<int>();
        foreach (var product in requestedProducts)
        {
            var productId = product.Id;
            var requestedAmount = productIdsToAmount[productId];
            if (requestedAmount > product.ProductCostItems.Count())
            {
                failedProductValidation.Add(productId);
            }
            else
            {
                totalAmountToBuy += product.Price * requestedAmount;
            }
        }
        
        if (failedProductValidation.Count > 0)
        {
            return (null ,new OperationValuedResult<PaymentSuccessModel, PaymentErrorModel>
            {
                IsSuccess = false,
                Error = new PaymentErrorModel
                {
                    FailedIds = failedProductValidation
                }
            });
        }

        return (totalAmountToBuy, null);
    }

    public async Task<OperationResult<PaymentErrorModel>> ProductPurchasePaymentFromBalance(
        ProductPurchase productPurchaseModel)
        => await RunAfterAmountCalculated(productPurchaseModel, 
            async (amount, productIdToAmount, requestedProducts) =>
            {
                var targetIdentity = _commerceContext.Identities
                    .Include(identities => identities.Payments)
                    .First(identity => identity.Email ==  productPurchaseModel.Email);

                //Should be propagated to view it's cinda event sourcing just to make easier to determine payment flow
                var balanceAmount = targetIdentity.Payments
                    .Where(payment => payment.PaymentStatus is PaymentStatus.Confirmed
                                      && payment.StoredPaymentType is StoredPaymentType.BalanceUpdate 
                                          or StoredPaymentType.ProductPaymentFromBalance)
                    .Select(payment => new {amount = payment.StoredPaymentType switch
                    {
                        StoredPaymentType.ProductPaymentFromBalance => -payment.PaymentAmount,
                        StoredPaymentType.BalanceUpdate => +payment.PaymentAmount
                    }}) 
                    .Sum(payment => payment.amount);

                //Usage sort of precision if not greater than we have enough amount
                // if (amount.TotalAmount > balanceAmount)
                // {
                //     return new OperationResult<PaymentErrorModel>()
                //     {
                //         IsSuccess = false,
                //         Error = new PaymentErrorModel
                //         {
                //             Reason = "Unable to buy products from balance if you don't have enough money"
                //         }
                //     };
                // }

                var paymentNotificationType = amount.VoucherUsed is not null
                    ? PaymentNotificationType.FromBalance | PaymentNotificationType.WithVoucher:
                    PaymentNotificationType.FromBalance;
                
                await _notificationService.NotifyAdminPurchase(new NotifyPurchaseModel
                {
                    CustomerEmail = productPurchaseModel.Email,
                    ProductPurchaseNotificationModels = requestedProducts.Select(product => new ProductPurchaseNotificationModel
                    {
                        Name = product.Name,
                        Amount = productIdToAmount[product.Id]
                    })
                }, paymentNotificationType );

                
                await PerformPaymentWithoutCrypto(targetIdentity,
                    amount, 
                    productIdToAmount, requestedProducts,
                    StoredPaymentType.ProductPaymentFromBalance);

                return new OperationResult<PaymentErrorModel>()
                {
                    IsSuccess = true
                };
            });

    private async Task PerformPaymentWithoutCrypto(IdentityDAO targetIdentity,
        PaymentAmount amount, Dictionary<int, int> productIdToAmount, List<ProductDAO> requestedProducts, StoredPaymentType paymentType)
    {
        var payment = new PaymentDAO
        {
            PaymentStatus = PaymentStatus.Confirmed,
            InvoiceCode = null,
            InvoiceUrl = null,
            RequestedProducts = Enumerable.Empty<RequestedProductToBuyDAO>(),
            StoredPaymentType = paymentType,
            DateOfCreation = DateTimeOffset.UtcNow,
            DateOfResolove = DateTimeOffset.UtcNow,
            Identity = targetIdentity,
            PaymentAmount = amount.TotalAmount,
            VoucherActivation = amount.VoucherUsed is not null? new VoucherActivationDAO
            {
                Voucher = amount.VoucherUsed,
                ActivationDate = DateTimeOffset.UtcNow
            }: null,
             
        };
        _commerceContext.Payments.Add(payment);

        //Updated amount

        var requestedProductIds = productIdToAmount.Keys.AsEnumerable();
        var matchedProductSailItems = await _commerceContext
            .ProductCostItems
            .Where(costItem => requestedProductIds.Contains(costItem.ProductId) && costItem.OwnedBy == null)
            .ToListAsync();


        Dictionary<int, List<ProductCostItemsDAO>> idToCostItems
            = MapToDictionary(matchedProductSailItems);


        List<ProductNotificationItem> notificationItems = new();
        foreach (var product in requestedProducts)
        {
            var requestedCount = productIdToAmount[product.Id];
            //Remove cost items that will be send to user
            var costItemsToSend
                = idToCostItems[product.Id]
                    .Take(requestedCount)
                    .ToList();
            
            costItemsToSend.ForEach(costItem =>
            {
                costItem.Payment = payment;
            });
            //Update count of items that available 
            notificationItems.Add(new ProductNotificationItem
            {
                ProductName = product.Name,
                ProductPriceForItem = product.Price,
                GoodsPayload = costItemsToSend.Select((item) => item.UserUnderstandableItem),
            });
        }

        var notificationModel = new GoodsNotificationModel
        {
            TargetEmail = targetIdentity.Email,
            NotificationItems = notificationItems,
        };
        await this._notificationService.NotifyGoodsReceive(notificationModel);
        await _commerceContext.SaveChangesAsync();
    }


    public async Task CompletePaymentsWithIds(IEnumerable<int> paymentIds)
    {
        using var transactionScope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.RepeatableRead
            }, TransactionScopeAsyncFlowOption.Enabled);

        var paymentToComplete = await LoadPaymentsForIdsWithIdentityAndReservationInfo(paymentIds);

        var usedCostItems = new HashSet<int>();
        foreach (var payment in paymentToComplete)
        {
            if (payment.PaymentStatus == PaymentStatus.Confirmed)
            {
                continue;
            }
            MarkPaymentCompleted(payment);
            //If payment not from wallet jumped here it will be strange flow and we could fix it in the future
            if (payment.StoredPaymentType is StoredPaymentType.ProductPaymentFromWallet)
            {
                //In that case we should perform notification and clear reservations
                List<ProductNotificationItem> notificationItems = new();
                
                foreach (var requestProduct in payment.RequestedProducts)
                {
                    var notificationItem = HandleRequestedProduct(requestProduct, payment, usedCostItems);
                    if (notificationItem is not null)
                    {
                        notificationItems.Add(notificationItem);
                    }
                }

                if (notificationItems.Count > 0)
                {
                    await _notificationService.NotifyGoodsReceive(new GoodsNotificationModel
                    {
                        TargetEmail = payment.Identity.Email,
                        NotificationItems = notificationItems
                    });
                }

                if (payment.VoucherActivationId is not null)
                {
                    //Voucher have was used between payment
                    await _notificationService.NotifyAdminPurchase(new NotifyPurchaseModel
                    {
                        CustomerEmail = payment.Identity.Email,
                        ProductPurchaseNotificationModels = notificationItems.Select(item =>
                            new ProductPurchaseNotificationModel
                            {
                                Name = item.ProductName,
                                Amount = item.GoodsPayload.Count()
                            })
                    }, PaymentNotificationType.WithVoucher);
                }
            };
        }
        await _commerceContext.SaveChangesAsync();
        transactionScope.Complete();
    }

    private ProductNotificationItem? HandleRequestedProduct(
        RequestedProductToBuyDAO requestProduct,
        PaymentDAO payment,
        HashSet<int> usedCostItems)
    {

        Func<ProductCostItemsDAO, bool> IsCostItemValid
            = costItem => costItem.OwnedBy == null 
                          && !usedCostItems.Contains(costItem.Id);
            

        int amountRequested = requestProduct.ItemsCountRequested;
        var freeCostItems = requestProduct.Product
            .ProductCostItems
            .Where(IsCostItemValid)
            .ToList();
        var freeCostItemsCount = freeCostItems.Count;
        if (amountRequested > freeCostItemsCount)
        {
            var countOfItemsToReturn = amountRequested - freeCostItemsCount;
            var productPrice = requestProduct.Product.Price;
            var totalAmountToReturnToBalance = productPrice * countOfItemsToReturn;
            this.UpdateBalance(new PerformUpdateBalancePayment
            {
                Amount = totalAmountToReturnToBalance,
                Email = payment.Identity.Email,
                RequestedPaymentStatus = PaymentStatus.Confirmed
            });
        }

        int countOfItemsToTake = Math.Min(amountRequested, freeCostItemsCount);
        var costItemsToTake = freeCostItems
            .Take(countOfItemsToTake)
            .ToList();
        costItemsToTake.ForEach(costItem => usedCostItems.Add(costItem.Id));
        payment.ProductCostItems = (payment.ProductCostItems ?? Enumerable.Empty<ProductCostItemsDAO>())
            .Union(costItemsToTake).ToList();
        if (countOfItemsToTake == 0)
        {
            return null;
        }
        return new ProductNotificationItem
        {
            GoodsPayload = costItemsToTake.Select(costItem => costItem.UserUnderstandableItem),
            ProductName = requestProduct.Product.Name,
            ProductPriceForItem = requestProduct.Product.Price
        };
    }

    private static void MarkPaymentCompleted(PaymentDAO payment)
    {
        payment.PaymentStatus = PaymentStatus.Confirmed;
        payment.DateOfResolove = DateTimeOffset.UtcNow;
    }

    public async Task RevertPaymentsWithIds(IEnumerable<int> paymentIds)
     {
         var paymentsToRevert = await LoadPaymentsForIdsWithIdentityAndReservationInfo(paymentIds);
         foreach (var payment in paymentsToRevert)
         {
             payment.PaymentStatus = PaymentStatus.Rejected;
             payment.DateOfResolove = DateTimeOffset.UtcNow;

             if (payment.VoucherActivation is not null)
             {
                 VoucherActivationDAO activationToRemove = payment.VoucherActivation;
                 payment.VoucherActivation = null;
                 _commerceContext.VoucherActivations.Remove(activationToRemove);
             }
         }
        
         await _commerceContext.SaveChangesAsync();
     }

     private Task<List<PaymentDAO>> LoadPaymentsForIdsWithIdentityAndReservationInfo(IEnumerable<int> requestedPaymentIds)
     {
         return _commerceContext.Payments
             .Where(payment => requestedPaymentIds.Contains(payment.Id))
             .Include(payment => payment.Identity)
             .Include(payment => payment.RequestedProducts)
             .ThenInclude(requestedProduct => requestedProduct.Product)
             .ThenInclude(product => product.ProductCostItems)
             .Include(payment => payment.VoucherActivation)
             .ToListAsync();
     }

    
    private Dictionary<int, List<ProductCostItemsDAO>> MapToDictionary(IEnumerable<ProductCostItemsDAO> purchaseItems)
    {
        var productIdToPurchaseItem = new Dictionary<int, List<ProductCostItemsDAO>>();
        foreach (var purchaseItem in purchaseItems)
        {
            if (productIdToPurchaseItem.TryGetValue(purchaseItem.ProductId, out var list))
            {
                list.Add(purchaseItem);
            }
            else
            {
                productIdToPurchaseItem.Add(purchaseItem.ProductId, new List<ProductCostItemsDAO>{purchaseItem});
            }
        }

        return productIdToPurchaseItem;
    }
    
    private async Task<TResult> RunAfterAmountCalculated<TResult>(ProductPurchase purchaseModel, 
        Func<PaymentAmount, Dictionary<int,int>, List<ProductDAO>, Task<TResult>> runAfterCalculation)
    where TResult: OperationResult<PaymentErrorModel>
    {
        using var transactionScope = new TransactionScope(TransactionScopeOption.Required,
            new TransactionOptions()
            {
                IsolationLevel = IsolationLevel.RepeatableRead
            }, TransactionScopeAsyncFlowOption.Enabled);

        var (productIdsToAmount, requestedProducts) = 
            await FetchInitialProductInfo(purchaseModel.ProductsToPurchase);

        var (requestedCount, result) = PerformPaymentCalculationAndValidation(productIdsToAmount, requestedProducts);
        if (result is not null)
        {
            transactionScope.Complete();
            return (TResult)result;
        }
        double totalAmountToBuy = requestedCount!.Value;

        var resultAmount = new PaymentAmount(null, totalAmountToBuy);
        if (purchaseModel.Voucher is not null and not "")
        {
            var voucherVerificationResult
                = await _voucherService.ApplyVoucher(new VoucherApplicationModel
                {
                     UserEmail = purchaseModel.Email,
                     Voucher = purchaseModel.Voucher,
                     AmountBeforeApplication = totalAmountToBuy
                });
            if (!voucherVerificationResult.IsSuccess)
            {
                return (TResult)(new OperationResult<PaymentErrorModel>()
                {
                    IsSuccess = false,
                    Error = new PaymentErrorModel
                    {
                        Reason = voucherVerificationResult.Error!,
                        FailedIds = Enumerable.Empty<int>()
                    }
                });
            }

            var nonNullableResult = voucherVerificationResult.Result!;
            resultAmount = new PaymentAmount(nonNullableResult.VoucherUsed!, nonNullableResult.ResultAmount);
        }

        var afterCalculationResult = await runAfterCalculation(resultAmount, productIdsToAmount, requestedProducts);
        
        await _commerceContext.SaveChangesAsync();
        transactionScope.Complete();
        return afterCalculationResult;
    }
}