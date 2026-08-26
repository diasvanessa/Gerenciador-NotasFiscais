using Faturamento.ApiEndpoints;
using Faturamento.Application;
using Faturamento.Application.UseCases;
using Faturamento.Data;
using Faturamento.Infrastructure;
using Faturamento.Infrastructure.Repositories;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<NotaFiscalContext>();
builder.Services.AddScoped<INotaFiscalRepository, NotaFiscalRepository>();
builder.Services.AddScoped<IEstoqueService, EstoqueService>();
builder.Services.AddScoped<ImprimirNotaFiscal>();
builder.Services.AddScoped<CriarNotaFiscal>(); 

var estoqueApiUrl = builder.Configuration["EstoqueApiUrl"] ?? "http://localhost:5032";

builder.Services.AddHttpClient<IEstoqueService, EstoqueService>(client =>
{
    client.BaseAddress = new Uri(estoqueApiUrl);
})
.AddPolicyHandler(ConfigurarPoliticaDeRetry())
.AddPolicyHandler(ConfigurarPoliticaDeCircuitBreaker())
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
//app.UseHttpsRedirection();

app.MapHealthChecks("/health");
app.FaturamentoEndpointsMap();

app.Run();

static IAsyncPolicy<HttpResponseMessage> ConfigurarPoliticaDeRetry()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() 
        .WaitAndRetryAsync(
            retryCount: 3, 
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1)), // 1s, 2s, 4s
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                var motivo = outcome.Exception?.Message ?? $"HTTP {(int?)outcome.Result?.StatusCode}";
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[Polly Retry] Falha na comunicação com Estoque ({motivo}). Tentativa {retryAttempt} de 3. Aguardando {timespan.TotalSeconds}s...");
                Console.ResetColor();
            });
}

static IAsyncPolicy<HttpResponseMessage> ConfigurarPoliticaDeCircuitBreaker()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 2,
            durationOfBreak: TimeSpan.FromSeconds(15),
            onBreak: (outcome, breakDelay) =>
            {
                var motivo = outcome.Exception?.Message ?? $"HTTP {(int?)outcome.Result?.StatusCode}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Polly Circuit Breaker] ⚡ CIRCUITO ABERTO! 2 falhas consecutivas detectadas ({motivo}). Interrompendo chamadas por {breakDelay.TotalSeconds}s para evitar sobrecarga.");
                Console.ResetColor();
            },
            onReset: () =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Polly Circuit Breaker] 🟢 CIRCUITO FECHADO! Comunicação com o Estoque restabelecida com sucesso.");
                Console.ResetColor();
            },
            onHalfOpen: () =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[Polly Circuit Breaker] 🟡 CIRCUITO SEMI-ABERTO! Testando requisição de sondagem para o Estoque...");
                Console.ResetColor();
            });
}
