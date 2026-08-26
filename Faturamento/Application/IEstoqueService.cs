namespace Faturamento.Application;

public interface IEstoqueService
{
    Task BaixarSaldoAsync(string codigoProduto, int quantidade);
    Task EstornarSaldoAsync(string codigoProduto, int quantidade);
}