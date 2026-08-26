using Faturamento.Application;
using Faturamento.Data;
using Faturamento.Domain;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Infrastructure.Repositories;

public class NotaFiscalRepository : INotaFiscalRepository
{
    private readonly NotaFiscalContext _context;

    public NotaFiscalRepository(NotaFiscalContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(NotaFiscal notaFiscal)
    {
        await _context.NotasFiscais.AddAsync(notaFiscal);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(NotaFiscal notaFiscal)
    {
        _context.NotasFiscais.Update(notaFiscal);
        await _context.SaveChangesAsync();
    }

    public async Task<NotaFiscal?> ObterPorIdAsync(Guid id)
    {
        return await _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NotaFiscal?> ObterPorNumeroAsync(int numero)
    {
        return await _context.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Numero == numero);
    }

    public async Task<IEnumerable<NotaFiscal>> ListarAsync()
    {
        return await _context.NotasFiscais
            .Include(n => n.Itens)
            .ToListAsync();
    }

    public async Task<int> ObterProximoNumeroAsync()
    {
        return (await _context.NotasFiscais.MaxAsync(n => (int?)n.Numero) ?? 0) + 1;
    }
}
