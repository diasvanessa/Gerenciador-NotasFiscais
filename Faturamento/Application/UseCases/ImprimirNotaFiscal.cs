using Faturamento.Domain;
using Faturamento.Domain.Exceptions;

namespace Faturamento.Application.UseCases;

public record ImprimirNotaFiscalRequest(int Numero);

public class ImprimirNotaFiscal
{
    private readonly INotaFiscalRepository _repository;
    private readonly IEstoqueService _estoqueService;

    public ImprimirNotaFiscal(INotaFiscalRepository repository, IEstoqueService estoqueService)
    {
        _repository = repository;
        _estoqueService = estoqueService;
    }

    public async Task ExecutarAsync(ImprimirNotaFiscalRequest request)
    {
        var nota = await _repository.ObterPorNumeroAsync(request.Numero)
            ?? throw new NotaFiscalNaoEncontradaException(request.Numero);

        nota.ValidarPodeSerFechada();

        var itensBaixados = new List<ItemNotaFiscal>();

        try
        {
            foreach (var item in nota.Itens)
            {
                await _estoqueService.BaixarSaldoAsync(item.CodigoProduto, item.Quantidade);
                itensBaixados.Add(item);
            }
        }
        catch (Exception ex)
        {
            // Executa transação compensatória (Saga Rollback) para todos os itens já debitados
            if (itensBaixados.Any())
            {
                Console.WriteLine($"[Saga Rollback] Estornando {itensBaixados.Count} item(ns) previamente debitados da NF {request.Numero}...");
                foreach (var item in itensBaixados)
                {
                    await _estoqueService.EstornarSaldoAsync(item.CodigoProduto, item.Quantidade);
                }
            }

            throw new FalhaAoImprimirNotaFiscalException(request.Numero, ex, estornoExecutado: itensBaixados.Any());
        }

        nota.Fechar();
        await _repository.AtualizarAsync(nota);
    }
}