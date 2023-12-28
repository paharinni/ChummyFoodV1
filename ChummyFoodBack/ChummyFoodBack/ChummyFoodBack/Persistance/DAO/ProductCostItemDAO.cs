using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;

[Table("ProductCostItems")]
[PrimaryKey(nameof(Id))]
public class ProductCostItemsDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string UserUnderstandableItem { get; set; }

    [ForeignKey(nameof(Payment))]
    public int? OwnedBy { get; set; }

    public PaymentDAO Payment { get; set; }
    public int ProductId { get; set; }

    public ProductDAO Product { get; set; }
}