using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ecommerce.Data;

public sealed class StoreDbContextFactory : IDesignTimeDbContextFactory<StoreDbContext>
{
    public StoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StoreDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EcommerceStore;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new StoreDbContext(options);
    }
}
