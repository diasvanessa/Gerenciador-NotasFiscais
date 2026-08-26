using Faturamento.Domain.Exceptions;

namespace Faturamento.Domain;

public class ItemNotaFiscal
{
    public Guid Id { get; private set; }
    public string CodigoProduto { get; private set; }
    public int Quantidade { get; private set; }

    public ItemNotaFiscal(string codigoProduto, int quantidade)
    {
        if (string.IsNullOrWhiteSpace(codigoProduto))
            throw new CodigoProdutoInvalidoException(codigoProduto);
        if (quantidade <= 0)
            throw new ItemQuantidadeInvalidaException(codigoProduto, quantidade);   

        Id = Guid.NewGuid();
        CodigoProduto = codigoProduto;
        Quantidade = quantidade;
    }
}