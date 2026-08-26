using Faturamento.Domain;

namespace Faturamento.Application;

public interface INotaFiscalRepository
{
    Task<NotaFiscal?> ObterPorIdAsync(Guid id);
    Task<NotaFiscal?> ObterPorNumeroAsync(int numero);
    Task<IEnumerable<NotaFiscal>> ListarAsync();
    Task<int> ObterProximoNumeroAsync();
    Task AdicionarAsync(NotaFiscal notaFiscal);
    Task AtualizarAsync(NotaFiscal notaFiscal);
}