using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Estoque.Application;

namespace Estoque.Infrastructure.Services;

public class IaVisionService : IIaVisionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IaVisionService> _logger;

    public IaVisionService(HttpClient httpClient, IConfiguration configuration, ILogger<IaVisionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> ReconhecerImagemAsync(Stream imageStream, string contentType, string fileName)
    {
        var apiKey = _configuration["IA:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("SUA_") || apiKey.StartsWith("YOUR_"))
        {
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                  ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                  ?? Environment.GetEnvironmentVariable("IA_API_KEY")
                  ?? Environment.GetEnvironmentVariable("IA__ApiKey");
        }

        var provider = _configuration["IA:Provider"] ?? "Gemini";

        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();
        var base64Image = Convert.ToBase64String(imageBytes);

        if (!string.IsNullOrWhiteSpace(apiKey) && provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return await ReconhecerComOpenAiAsync(base64Image, contentType, apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar OpenAI Vision. Usando fallback.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(apiKey) && (provider.Contains("Gemini", StringComparison.OrdinalIgnoreCase) || provider.Equals("Google", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                return await ReconhecerComGeminiAsync(base64Image, contentType, apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao consultar Gemini Vision. Usando fallback.");
            }
        }
        else
        {
            _logger.LogInformation("Nenhuma chave de API configurada para IA (Gemini/OpenAI). Utilizando sugestão inteligente de fallback.");
        }

        return GerarSugestaoFallback(fileName);
    }

    private async Task<string> ReconhecerComOpenAiAsync(string base64Image, string contentType, string apiKey)
    {
        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Identifique o produto principal nesta foto e retorne apenas o nome comercial direto em português (exemplo: 'Tênis Esportivo Casual', 'Caneca Térmica Inox'). Responda apenas com o nome, sem aspas." },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{contentType};base64,{base64Image}"
                            }
                        }
                    }
                }
            },
            max_tokens = 50
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(requestMessage);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI retornou erro {StatusCode}: {Error}", response.StatusCode, json);
            throw new HttpRequestException($"Erro na OpenAI ({response.StatusCode}): {json}");
        }

        var doc = JsonNode.Parse(json);
        var resultado = doc?["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();

        return !string.IsNullOrWhiteSpace(resultado) ? resultado : "Produto Identificado por IA";
    }

    private async Task<string> ReconhecerComGeminiAsync(string base64Image, string contentType, string apiKey)
    {
        var modelNames = new[] { "gemini-3.6-flash", "gemini-3.5-flash-lite" };
        var requestBody = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = "Identifique o produto principal nesta foto e retorne apenas o nome comercial direto em português (exemplo: 'Tênis Esportivo Casual', 'Caneca Inox Térmica'). Responda apenas com o nome curto do produto, sem introdução." },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType,
                                data = base64Image
                            }
                        }
                    }
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        foreach (var model in modelNames)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsync(url, jsonContent);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var doc = JsonNode.Parse(json);
                var resultado = doc?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(resultado))
                {
                    return resultado;
                }
            }
            else
            {
                _logger.LogWarning("Google Gemini ({Model}) retornou erro {StatusCode}: {Error}", model, response.StatusCode, json);
            }
        }

        throw new HttpRequestException("Nenhum modelo do Gemini respondeu com sucesso. Verifique a chave de API em GEMINI_API_KEY ou appsettings.json.");
    }

    private string GerarSugestaoFallback(string fileName)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();

        if (nameWithoutExt.Contains("tenis") || nameWithoutExt.Contains("shoe") || nameWithoutExt.Contains("calcado"))
            return "Tênis Esportivo Casual";
        if (nameWithoutExt.Contains("camisa") || nameWithoutExt.Contains("shirt") || nameWithoutExt.Contains("polo"))
            return "Camiseta Polo Performance Dry-Fit";
        if (nameWithoutExt.Contains("moletom") || nameWithoutExt.Contains("hoodie"))
            return "Moletom Sportswear Casual";
        if (nameWithoutExt.Contains("jaqueta") || nameWithoutExt.Contains("jacket"))
            return "Jaqueta Corta-Vento Esportiva";

        return "Produto Comercial em Destaque";
    }
}
