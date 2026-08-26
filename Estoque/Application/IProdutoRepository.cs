using Estoque.Domain;

namespace Estoque.Application;

public interface IProdutoRepository
{
    Task<Produto?> ObterPorIdAsync(Guid id);
    Task<Produto?> ObterPorCodigoAsync(string codigo);
    Task<List<Produto>> ListarAsync();
    Task AdicionarAsync(Produto produto);
    Task AtualizarAsync(Produto produto);
}