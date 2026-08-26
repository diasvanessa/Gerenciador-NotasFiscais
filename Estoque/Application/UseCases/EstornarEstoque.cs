using Estoque.Domain.Exceptions;

namespace Estoque.Application.UseCases;

public class EstornarEstoque
{
    private readonly IProdutoRepository _repository;
    public EstornarEstoque(IProdutoRepository repository) => _repository = repository;

    public async Task ExecutarAsync(AtualizarEstoqueRequest request)
    {
        var produto = await _repository.ObterPorCodigoAsync(request.Codigo)
            ?? throw new ProdutoNaoEncontradoException(request.Codigo);

        produto.AdicionarSaldo(request.QuantidadeUtilizada);
        await _repository.AtualizarAsync(produto);
    }
}