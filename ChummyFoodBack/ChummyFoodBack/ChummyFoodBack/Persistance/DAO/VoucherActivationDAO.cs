using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChummyFoodBack.Persistance.DAO;


[PrimaryKey(nameof(Id))]
[Table("VoucherActivation")]
public class VoucherActivationDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTimeOffset ActivationDate { get; set; }


    [ForeignKey(nameof(Voucher))]
    public int VoucherId { get; set; }

    public VoucherDAO Voucher { get; set; }

    [ForeignKey(nameof(PaymentDAO))]
    public int PaymentId { get; set; }

    public PaymentDAO Payment { get; set; }
}