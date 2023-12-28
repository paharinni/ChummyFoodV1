using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;

[Table("RequestedProductToBuy")]
[PrimaryKey(nameof(Id))]
public class RequestedProductToBuyDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ItemsCountRequested { get; set; }

    [ForeignKey(nameof(Product))]
    public int ProductId { get; set; }

    public ProductDAO Product { get; set; }
}
