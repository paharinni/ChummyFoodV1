using ChummyFoodBack.Persistance.DAO;
using Microsoft.EntityFrameworkCore;

namespace ChummyFoodBack.Persistance;

public class CommerceContext : DbContext
{
    public DbSet<IdentityDAO> Identities { get; set; }

    public DbSet<RestoreCodeDAO> RestoreCodes { get; set; }
    public DbSet<PaymentDAO> Payments { get; set; }

    public DbSet<CategoryDAO> Categories { get; set; }
    public DbSet<RoleDao> Roles { get; set; }

    public DbSet<ProductDAO> Products { get; set; }
    public DbSet<SiteDisplayDAO> SiteDisplay { get; set; }
    public DbSet<VoucherDAO> Vouchers { get; set; }

    public DbSet<VoucherActivationDAO> VoucherActivations { get; set; }

    public DbSet<ProductCostItemsDAO> ProductCostItems { get; set; }

    public CommerceContext(DbContextOptions<CommerceContext> options) : base(options) { }
}