using Estoque.Application;
using Estoque.Data;
using Estoque.Domain;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Infrastructure.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly ProdutoContext _context;

    public ProdutoRepository(ProdutoContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Produto produto)
    {
        await _context.Produtos.AddAsync(produto);
        await _context.SaveChangesAsync();
    }

    public async Task<Produto?> ObterPorIdAsync(Guid id)
    {
        return await _context.Produtos.FindAsync(id);
    }

    public async Task<Produto?> ObterPorCodigoAsync(string codigo)
    {
        return await _context.Produtos.FirstOrDefaultAsync(p => p.Codigo == codigo);
    }

    public async Task<List<Produto>> ListarAsync()
    {
        return await _context.Produtos.ToListAsync();
    }

    public async Task AtualizarAsync(Produto produto)
    {
        _context.Produtos.Update(produto);
        try{
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new Exception("O saldo deste produto foi atualizado por outra transação simultânea. Por favor, tente emitir a nota novamente.");
        }
    }
}
