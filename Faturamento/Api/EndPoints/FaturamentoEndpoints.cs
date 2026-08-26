using Faturamento.Application;
using Faturamento.Application.UseCases;
using Faturamento.Domain.Exceptions;

namespace Faturamento.ApiEndpoints;

public static class FaturamentoEndpoints
{
    public static void FaturamentoEndpointsMap(this WebApplication app)
    {
        var route = app.MapGroup("api/faturamento");

        route.MapPost("", async (CriarNotaFiscalRequest req, CriarNotaFiscal useCase) =>
        {
            try
            {
                var nota = await useCase.ExecutarAsync(req);
                return Results.Created($"/api/faturamento/{nota.Numero}", nota);
            }
            catch (NotaFiscalVaziaException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
            catch (ItemJaAdicionadoException ex)
            {
                return Results.Conflict(new { mensagem = ex.Message });
            }
            catch (ItemQuantidadeInvalidaException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
            catch (CodigoProdutoInvalidoException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
        });

        route.MapPost("{numero:int}/imprimir", async (int numero, ImprimirNotaFiscal useCase) =>
        {
            try
            {
                var request = new ImprimirNotaFiscalRequest(numero);
                await useCase.ExecutarAsync(request);
                return Results.Ok(new { mensagem = $"Nota fiscal nº {numero} impressa com sucesso!" });
            }
            catch (NotaFiscalNaoEncontradaException ex)
            {
                return Results.NotFound(new { mensagem = ex.Message });
            }
            catch (NotaFiscalNaoPodeSerImpressaException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
            catch (NotaFiscalSemItensException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
            catch (FalhaAoImprimirNotaFiscalException ex)
            {
                if (ex.InnerException is EstoqueIndisponivelException indispEx)
                {
                    return Results.Json(new
                    {
                        mensagem = ex.Message,
                        detalhes = indispEx.Message,
                        servicoAfetado = "Estoque",
                        status = "Indisponivel",
                        recuperavel = true,
                        estornoExecutado = ex.EstornoExecutado
                    }, statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                if (ex.InnerException is EstoqueFalhaValidacaoException validEx)
                {
                    return Results.UnprocessableEntity(new
                    {
                        mensagem = ex.Message,
                        detalhes = validEx.Message,
                        servicoAfetado = "Estoque",
                        status = "ValidacaoFalhou",
                        recuperavel = true,
                        estornoExecutado = ex.EstornoExecutado
                    });
                }

                return Results.UnprocessableEntity(new
                {
                    mensagem = ex.Message,
                    detalhes = ex.InnerException?.Message ?? "Erro inesperado ao processar integração com Estoque.",
                    servicoAfetado = "Estoque",
                    status = "Erro",
                    recuperavel = true,
                    estornoExecutado = ex.EstornoExecutado
                });
            }
        });

        route.MapGet("", async (INotaFiscalRepository repository) =>
        {
            var notas = await repository.ListarAsync();
            return Results.Ok(notas);
        });

        route.MapGet("{numero:int}", async (int numero, INotaFiscalRepository repository) =>
        {
            var nota = await repository.ObterPorNumeroAsync(numero);
            return nota is not null 
                ? Results.Ok(nota) 
                : Results.NotFound(new { mensagem = $"Nota fiscal nº {numero} não encontrada." });
        });

        route.MapGet("status-dependencias", async (IConfiguration config) =>
        {
            var estoqueUrl = config["EstoqueApiUrl"] ?? "http://localhost:5032";
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            
            bool estoqueOk = false;
            string statusEstoque = "Offline";

            try
            {
                var response = await client.GetAsync($"{estoqueUrl}/health");
                estoqueOk = response.IsSuccessStatusCode;
                statusEstoque = estoqueOk ? "Online" : $"HTTP {(int)response.StatusCode}";
            }
            catch (Exception ex)
            {
                statusEstoque = $"Falha de Conexão ({ex.Message})";
            }

            return Results.Ok(new
            {
                faturamento = "Online",
                estoque = new
                {
                    url = estoqueUrl,
                    online = estoqueOk,
                    status = statusEstoque
                },
                timestamp = DateTime.UtcNow
            });
        });
    }
}
