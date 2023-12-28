using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;

[Table("OrderStatus")]
[PrimaryKey(nameof(Id))]
public class OrderStatusDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }

    public IEnumerable<ProductDAO> OrderStatuses { get; set; }
}
