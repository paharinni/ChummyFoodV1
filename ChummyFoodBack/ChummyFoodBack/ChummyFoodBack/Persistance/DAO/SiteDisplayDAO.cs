using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance.DAO;


[Table("SiteDisplay")]
[PrimaryKey(nameof(Id))]
public class SiteDisplayDAO
{

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string HeaderText { get; set; }
}
