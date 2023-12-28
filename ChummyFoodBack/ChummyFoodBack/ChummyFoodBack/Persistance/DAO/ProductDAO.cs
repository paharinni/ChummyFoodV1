using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChummyFoodBack.Persistance.DAO;

[Table("Product")]
[PrimaryKey(nameof(Id))]
public class ProductDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }

    public double Price { get; set; }

    public string? UnitOfPrice { get; set; }

    public string ImageFileName { get; set; }

    public string ImageContentType { get; set; }

    public string? ProductDownText { get; set; }
    public int CategoryId { get; set; }

    public IEnumerable<ProductCostItemsDAO> ProductCostItems { get; set; } = new List<ProductCostItemsDAO>();

    public CategoryDAO Category { get; set; }
    public string ProductDescription { get; set; }
}
