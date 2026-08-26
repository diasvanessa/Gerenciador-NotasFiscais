using Faturamento.Domain;
using Faturamento.Domain.Exceptions;

namespace Faturamento.Application.UseCases;

public record ItemNotaFiscalRequest(string CodigoProduto, int Quantidade);
public record CriarNotaFiscalRequest(List<ItemNotaFiscalRequest> Itens);

public class CriarNotaFiscal
{
    private readonly INotaFiscalRepository _repository;

    public CriarNotaFiscal(INotaFiscalRepository repository)
    {
        _repository = repository;
    }

    public async Task<NotaFiscal> ExecutarAsync(CriarNotaFiscalRequest request)
    {
        if (!request.Itens.Any())
            throw new NotaFiscalVaziaException();

        var proximoNumero = await _repository.ObterProximoNumeroAsync();
        var notaFiscal = new NotaFiscal(proximoNumero);

        foreach (var item in request.Itens)
        {
            notaFiscal.AdicionarItem(item.CodigoProduto, item.Quantidade);
        }

        await _repository.AdicionarAsync(notaFiscal);
        return notaFiscal;
    }
}
