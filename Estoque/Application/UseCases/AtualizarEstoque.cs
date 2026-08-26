using Estoque.Domain.Exceptions;

namespace Estoque.Application.UseCases;

public record AtualizarEstoqueRequest(string Codigo, int QuantidadeUtilizada);

public class AtualizarEstoque
{
    private readonly IProdutoRepository _repository;

    public AtualizarEstoque(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecutarAsync(AtualizarEstoqueRequest request)
    {
        var produto = await _repository.ObterPorCodigoAsync(request.Codigo);
        if (produto is null)
            throw new ProdutoNaoEncontradoException(request.Codigo);

        produto.BaixarSaldo(request.QuantidadeUtilizada);

        await _repository.AtualizarAsync(produto);
    }
}