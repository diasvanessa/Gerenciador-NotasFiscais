using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Faturamento.Application;
using Faturamento.Domain.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Faturamento.Infrastructure;

public class EstoqueService : IEstoqueService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EstoqueService> _logger;

    public EstoqueService(HttpClient httpClient, ILogger<EstoqueService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task BaixarSaldoAsync(string codigoProduto, int quantidade)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"/api/produtos/{codigoProduto}/baixar-saldo", new { quantidade });

            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            string? mensagemErro = null;

            try
            {
                var json = JsonNode.Parse(body);
                mensagemErro = json?["mensagem"]?.GetValue<string>();
            }
            catch
            {
                mensagemErro = body;
            }

            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                throw new EstoqueFalhaValidacaoException(
                    !string.IsNullOrWhiteSpace(mensagemErro)
                        ? mensagemErro
                        : $"Falha ao baixar saldo do produto '{codigoProduto}' (HTTP {(int)response.StatusCode}).");
            }

            throw new EstoqueIndisponivelException(
                $"O serviço de Estoque retornou status {(int)response.StatusCode}: {mensagemErro}");
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "[Circuit Breaker] Circuito aberto ao tentar contatar Estoque para o produto {CodigoProduto}.", codigoProduto);
            throw new EstoqueIndisponivelException("O circuito de proteção (Circuit Breaker) está ABERTO devido a falhas consecutivas no microsserviço de Estoque.", ex);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "[Timeout] Tempo limite esgotado ao tentar contatar Estoque para o produto {CodigoProduto}.", codigoProduto);
            throw new EstoqueIndisponivelException("Tempo limite esgotado aguardando resposta do serviço de Estoque.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[Conexão] Falha de rede/conexão ao contatar Estoque para o produto {CodigoProduto}.", codigoProduto);
            throw new EstoqueIndisponivelException("Não foi possível conectar ao microsserviço de Estoque (offline ou inacessível).", ex);
        }
        catch (EstoqueFalhaValidacaoException)
        {
            throw;
        }
        catch (EstoqueIndisponivelException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao comunicar com o Estoque.");
            throw new EstoqueIndisponivelException("Erro inesperado na comunicação com o microsserviço de Estoque.", ex);
        }
    }

    public async Task EstornarSaldoAsync(string codigoProduto, int quantidade)
    {
        try
        {
            _logger.LogInformation("[Compensação] Executando estorno de {Quantidade} un para o produto '{CodigoProduto}'...", quantidade, codigoProduto);
            var response = await _httpClient.PatchAsJsonAsync(
                $"/api/produtos/{codigoProduto}/estornar-saldo", new { quantidade });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Compensação] Saldo estornado com sucesso para produto '{CodigoProduto}' (+{Quantidade}).", codigoProduto, quantidade);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[Compensação] Falha ao estornar produto '{CodigoProduto}': HTTP {Status} - {Body}", codigoProduto, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[CRÍTICO - Compensação Falhou] Falha irrecuperável ao estornar {Quantidade} un do produto '{CodigoProduto}'. Requer auditoria!", quantidade, codigoProduto);
        }
    }
}