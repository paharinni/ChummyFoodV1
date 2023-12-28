using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChummyFoodBack.Persistance.DAO;

[Table("Voucher")]
[PrimaryKey(nameof(Id))]
public class VoucherDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Voucher { get; set; }

    public string Currency { get; set; }

    public double Discount { get; set; }

    public string Description { get; set; }

    public IEnumerable<VoucherActivationDAO> VoucherActivations { get; set; } = new HashSet<VoucherActivationDAO>();
    public DateTimeOffset IssueDate { get; set; }

    public DateTimeOffset ExpiryDate { get; set; }
}