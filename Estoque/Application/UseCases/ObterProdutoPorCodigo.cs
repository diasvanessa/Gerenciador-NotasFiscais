using Estoque.Domain;
using Estoque.Domain.Exceptions;

namespace Estoque.Application.UseCases;

public class ObterProdutoPorCodigo
{
    private readonly IProdutoRepository _repository;

    public ObterProdutoPorCodigo(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Produto> ExecutarAsync(string codigo)
    {
        var produto = await _repository.ObterPorCodigoAsync(codigo);
        if (produto is null)
            throw new ProdutoNaoEncontradoException(codigo);

        return produto;
    }
}
