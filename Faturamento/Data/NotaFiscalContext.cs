using Microsoft.EntityFrameworkCore;
using Faturamento.Domain;

namespace Faturamento.Data;

public class NotaFiscalContext : DbContext
{
    public DbSet<NotaFiscal> NotasFiscais { get; set; } = null!;
    public DbSet<ItemNotaFiscal> ItensNotasFiscais { get; set; } = null!;

    override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
    {
        optionsBuilder.UseSqlite("Data Source=Faturamento.sqlite");
        base.OnConfiguring(optionsBuilder);
    }
}