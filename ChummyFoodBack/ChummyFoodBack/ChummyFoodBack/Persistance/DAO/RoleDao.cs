using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;

[PrimaryKey(nameof(Id))]
[Table("Role")]
public class RoleDao
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }
}
