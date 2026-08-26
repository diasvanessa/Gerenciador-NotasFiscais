using Estoque.Domain;

namespace Estoque.Application.UseCases;

public class ObterProdutos
{
    private readonly IProdutoRepository _repository;
    public ObterProdutos(IProdutoRepository repository) => _repository = repository;

    public async Task<List<Produto>> ExecutarAsync()
    {
        return await _repository.ListarAsync();
    }
}