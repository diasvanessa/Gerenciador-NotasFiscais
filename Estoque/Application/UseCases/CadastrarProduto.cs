using Estoque.Domain;
using Estoque.Domain.Exceptions;

namespace Estoque.Application.UseCases;

public record CadastrarProdutoRequest(string Codigo, string Descricao, int SaldoInicial, string? ImagemUrl = null);

public class CadastrarProduto
{
    private readonly IProdutoRepository _repository;

    public CadastrarProduto(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Produto> ExecutarAsync(CadastrarProdutoRequest request)
    {
        var produtoExistente = await _repository.ObterPorCodigoAsync(request.Codigo);
        if (produtoExistente is not null)
            throw new ProdutoJaCadastradoException(request.Codigo);

        var produto = new Produto(request.Codigo, request.Descricao, request.SaldoInicial, request.ImagemUrl);

        await _repository.AdicionarAsync(produto);

        return produto;
    }
}