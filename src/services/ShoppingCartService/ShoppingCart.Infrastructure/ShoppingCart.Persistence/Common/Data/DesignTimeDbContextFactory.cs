using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShoppingCart.Persistence.Common.Data;

/// <summary>Used by <c>dotnet ef</c> (migrations add / script / bundle); not used at runtime.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShoppingCartDbContext>
{
    public ShoppingCartDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ShoppingCartDb")
            ?? "Host=localhost;Port=5432;Database=shoppingcart;Username=postgres;Password=Your_password123";

        var options = new DbContextOptionsBuilder<ShoppingCartDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ShoppingCartDbContext(options);
    }
}
