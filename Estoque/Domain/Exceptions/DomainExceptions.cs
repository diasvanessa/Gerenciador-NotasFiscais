namespace Estoque.Domain.Exceptions;

public class SaldoInsuficienteException : Exception
{
    public SaldoInsuficienteException(string codigo, int saldoAtual, int quantidadeSolicitada)
        : base($"Saldo insuficiente para o produto '{codigo}'. Saldo atual: {saldoAtual}, solicitado: {quantidadeSolicitada}.")
    { }
}

public class ProdutoNaoEncontradoException : Exception
{
    public ProdutoNaoEncontradoException(string codigo)
        : base($"Produto com código '{codigo}' não foi encontrado.")
    { }
}

public class ProdutoJaCadastradoException : Exception
{
    public ProdutoJaCadastradoException(string codigo)
        : base($"Já existe um produto cadastrado com o código '{codigo}'.")
    { }
}