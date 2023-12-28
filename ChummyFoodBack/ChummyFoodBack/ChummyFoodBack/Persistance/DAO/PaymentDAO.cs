using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChummyFoodBack.Persistance.DAO;

public enum PaymentStatus
{
    WaitForPayment,
    //delete reservation
    Confirmed,
    //remove from reservation make available to buy)

    Rejected
}

public enum StoredPaymentType
{
    BalanceUpdate,
    ProductPaymentFromBalance,
    ProductPaymentFromWallet
}

[Table("Payment")]
public class PaymentDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    //Created, WaitingForApprove
    public PaymentStatus PaymentStatus { get; set; }

    public double PaymentAmount { get; set; }

    public StoredPaymentType StoredPaymentType { get; set; }

    public string? InvoiceCode { get; set; }

    public string? InvoiceUrl { get; set; }

    public DateTimeOffset DateOfCreation { get; set; }

    public DateTimeOffset? DateOfResolove { get; set; }

    [ForeignKey(nameof(VoucherActivation))]
    public int? VoucherActivationId { get; set; }

    public VoucherActivationDAO? VoucherActivation { get; set; }

    public IEnumerable<RequestedProductToBuyDAO> RequestedProducts { get; set; }
    public int IdentityId { get; set; }

    public IdentityDAO Identity { get; set; }

    public IEnumerable<ProductCostItemsDAO>? ProductCostItems { get; set; }
}
