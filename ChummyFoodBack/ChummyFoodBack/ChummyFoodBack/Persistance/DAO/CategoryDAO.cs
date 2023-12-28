using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;

[Table("Category")]
[PrimaryKey(nameof(Id))]
public class CategoryDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }

    public IEnumerable<ProductDAO> Products { get; set; }
}
