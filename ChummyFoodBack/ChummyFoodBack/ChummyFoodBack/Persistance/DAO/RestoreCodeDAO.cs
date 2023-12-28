using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;

[Table("RestoreCode")]
[PrimaryKey(nameof(Id))]
public class RestoreCodeDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string RestoreCode { get; set; }

    public DateTimeOffset Issued { get; set; }

    public DateTimeOffset Valid { get; set; }

    [ForeignKey(nameof(Identity))]
    public int IdentityId { get; set; }

    public IdentityDAO Identity { get; set; }
}
