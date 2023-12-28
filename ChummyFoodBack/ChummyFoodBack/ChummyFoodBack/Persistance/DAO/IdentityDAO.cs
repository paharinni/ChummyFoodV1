using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChummyFoodBack.Persistance.DAO;

[Table("Identity")]
[PrimaryKey(nameof(Id))]
public class IdentityDAO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string PasswordSalt { get; set; }

    public int RoleId { get; set; }
    
    public string Name { get; set; }
    
    public string Surname { get; set; }
    
    public int Age { get; set; }
    
    public string City { get; set; }
    
    public string Country { get; set; }
    
    public RoleDao RoleDao { get; set; }

    public List<PaymentDAO> Payments { get; set; } = new();

    public IEnumerable<VoucherActivationDAO> VoucherActivations = new HashSet<VoucherActivationDAO>();

    public IEnumerable<RestoreCodeDAO> RestoreCodes { get; set; }
}