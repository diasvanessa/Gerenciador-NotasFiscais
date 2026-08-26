namespace Faturamento.Domain.Exceptions;

public class FalhaAoImprimirNotaFiscalException : Exception
{
    public int NumeroNota { get; }
    public bool EstornoExecutado { get; }

    public FalhaAoImprimirNotaFiscalException(int numero, Exception inner, bool estornoExecutado = true)
        : base($"Não foi possível imprimir a Nota Fiscal nº {numero}. O estoque foi mantido consistente e a nota permanece aberta.", inner)
    {
        NumeroNota = numero;
        EstornoExecutado = estornoExecutado;
    }
}

public class EstoqueIndisponivelException : Exception
{
    public EstoqueIndisponivelException(string motivo, Exception? inner = null)
        : base($"O microsserviço de Estoque está temporariamente indisponível. {motivo}", inner) { }
}

public class EstoqueFalhaValidacaoException : Exception
{
    public EstoqueFalhaValidacaoException(string mensagem)
        : base(mensagem) { }
}

public class NotaFiscalFechadaException : Exception
{
    public NotaFiscalFechadaException(int numero)
        : base($"Nota fiscal {numero} está fechada e não pode ser alterada.") { }
}

public class NotaFiscalNaoPodeSerImpressaException : Exception
{
    public NotaFiscalNaoPodeSerImpressaException(int numero, StatusNotaFiscal status)
        : base($"Nota fiscal {numero} não pode ser impressa. Status atual: {status}.") { }
}

public class NotaFiscalSemItensException : Exception
{
    public NotaFiscalSemItensException(int numero)
        : base($"Nota fiscal {numero} não possui itens e não pode ser fechada.") { }
}

public class ItemJaAdicionadoException : Exception
{
    public ItemJaAdicionadoException(string codigoProduto)
        : base($"O produto '{codigoProduto}' já foi adicionado a esta nota.") { }
}

public class NotaFiscalVaziaException : Exception
{
    public NotaFiscalVaziaException()
        : base("A nota fiscal deve conter ao menos um item.") { }
}

public class ItemQuantidadeInvalidaException : Exception{
    public ItemQuantidadeInvalidaException(string codigoProduto, int quantidade)
        : base($"A quantidade do produto '{codigoProduto}' deve ser maior que zero.") { }
}

public class CodigoProdutoInvalidoException : Exception{
    public CodigoProdutoInvalidoException(string codigoProduto)
        : base($"O código do produto '{codigoProduto}' é inválido.") { }
}

public class NotaFiscalNaoEncontradaException : Exception
{
    public NotaFiscalNaoEncontradaException(int numero)
        : base($"Nota fiscal nº {numero} não foi encontrada.") { }
}
