using Microsoft.EntityFrameworkCore;
using Estoque.Domain;

namespace Estoque.Data;

public class ProdutoContext : DbContext
{
    public DbSet<Produto> Produtos { get; set; } = null!;

    override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
    {
        optionsBuilder.UseSqlite("Data Source=Estoque.sqlite");
        base.OnConfiguring(optionsBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Produto>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}