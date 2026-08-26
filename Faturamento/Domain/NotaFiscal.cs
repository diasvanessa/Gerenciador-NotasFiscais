using Faturamento.Domain.Exceptions;

namespace Faturamento.Domain;

public enum StatusNotaFiscal
{
    Aberta,
    Fechada
}


public class NotaFiscal
{
    public Guid Id { get; private set; }
    public int Numero { get; private set; }
    public StatusNotaFiscal Status { get; private set; }

    private readonly List<ItemNotaFiscal> _itens = new();
    public IReadOnlyCollection<ItemNotaFiscal> Itens => _itens.AsReadOnly();

    public NotaFiscal(int numero)
    {
        if (numero <= 0)
            throw new ArgumentException("Número da nota deve ser maior que zero.");

        Id = Guid.NewGuid();
        Numero = numero;
        Status = StatusNotaFiscal.Aberta;
    }

    public void AdicionarItem(string codigoProduto, int quantidade)
    {
        if (Status != StatusNotaFiscal.Aberta)
            throw new NotaFiscalFechadaException(Numero);

        if (_itens.Any(i => i.CodigoProduto == codigoProduto))
            throw new ItemJaAdicionadoException(codigoProduto);

        _itens.Add(new ItemNotaFiscal(codigoProduto, quantidade));
    }

    private void GarantirPodeSerFechada()
    {
        if (Status != StatusNotaFiscal.Aberta)
            throw new NotaFiscalNaoPodeSerImpressaException(Numero, Status);

        if (_itens.Count == 0)
            throw new NotaFiscalSemItensException(Numero);
    }

    public void ValidarPodeSerFechada() => GarantirPodeSerFechada();

    public void Fechar()
    {
        GarantirPodeSerFechada();
        Status = StatusNotaFiscal.Fechada;
    }
}