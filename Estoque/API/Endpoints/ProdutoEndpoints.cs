using Estoque.Application.UseCases;
using Estoque.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.API.Endpoints;

public record AlterarSaldoBody(int Quantidade);

public static class ProdutoEndpoints
{
    public static void ProdutoEndpointsMap(this WebApplication app)
    {
        var route = app.MapGroup("api/produtos");

        route.MapGet("", async (ObterProdutos useCase) => 
        {
            var produtos = await useCase.ExecutarAsync();
            return Results.Ok(produtos);
        });

        route.MapGet("{codigo}", async (string codigo, ObterProdutoPorCodigo useCase) => 
        {
            try
            {
                var produto = await useCase.ExecutarAsync(codigo);
                return Results.Ok(produto);
            }
            catch (ProdutoNaoEncontradoException ex)
            {
                return Results.NotFound(new { mensagem = ex.Message });
            }
        });


        route.MapPost("", async (CadastrarProdutoRequest req, CadastrarProduto useCase) => 
        {
            try
            {
                var produto = await useCase.ExecutarAsync(req);
                return Results.Created($"/api/produtos/{produto.Id}", produto);
            }
            catch (ProdutoJaCadastradoException ex)
            {
                return Results.Conflict(new { mensagem = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
        });

        route.MapPatch("{codigo}/baixar-saldo", async (string codigo, [FromBody] AlterarSaldoBody body, AtualizarEstoque useCase) =>
        {
            try
            {
                var req = new AtualizarEstoqueRequest(codigo, body.Quantidade);
                await useCase.ExecutarAsync(req);
                return Results.Ok(new { mensagem = "Saldo baixado com sucesso." });
            }
            catch (ProdutoNaoEncontradoException ex)
            {
                return Results.NotFound(new { mensagem = ex.Message });
            }
            catch (SaldoInsuficienteException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
        });

        route.MapPatch("{codigo}/estornar-saldo", async (string codigo, [FromBody] AlterarSaldoBody body, EstornarEstoque useCase) =>
        {
            try
            {
                var req = new AtualizarEstoqueRequest(codigo, body.Quantidade);
                await useCase.ExecutarAsync(req);
                return Results.Ok(new { mensagem = "Saldo estornado com sucesso." });
            }
            catch (ProdutoNaoEncontradoException ex)
            {
                return Results.NotFound(new { mensagem = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { mensagem = ex.Message });
            }
        });

        route.MapPost("upload-imagem", async (IFormFile? arquivo, IWebHostEnvironment env, HttpContext httpContext) =>
        {
            if (arquivo is null || arquivo.Length == 0)
                return Results.BadRequest(new { mensagem = "Nenhum arquivo de imagem foi enviado." });

            var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();

            if (!extensoesPermitidas.Contains(extensao))
                return Results.BadRequest(new { mensagem = "Formato inválido. Use JPG, PNG, WEBP ou GIF." });

            var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "images");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var nomeArquivo = $"{Guid.NewGuid()}{extensao}";
            var caminhoCompleto = Path.Combine(uploadsFolder, nomeArquivo);

            using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var urlPublica = $"{baseUrl}/images/{nomeArquivo}";

            return Results.Ok(new { url = urlPublica, nomeArquivo });
        }).DisableAntiforgery();

        app.MapPost("api/ia/reconhecer-imagem", async (IFormFile? arquivo, Estoque.Application.IIaVisionService iaService) =>
        {
            if (arquivo is null || arquivo.Length == 0)
                return Results.BadRequest(new { mensagem = "Nenhum arquivo de imagem foi enviado." });

            var contentType = arquivo.ContentType ?? "image/jpeg";
            using var stream = arquivo.OpenReadStream();

            var sugestao = await iaService.ReconhecerImagemAsync(stream, contentType, arquivo.FileName);

            return Results.Ok(new { sugestao });
        }).DisableAntiforgery();
    }
}
