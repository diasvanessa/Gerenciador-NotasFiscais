using Estoque.API.Endpoints;
using Estoque.Application;
using Estoque.Application.UseCases;
using Estoque.Data;
using Estoque.Infrastructure.Repositories;
using Estoque.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Garante a existência da pasta wwwroot para arquivos estáticos e uploads
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRootPath, "images"));
builder.Environment.WebRootPath = webRootPath;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<ProdutoContext>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<CadastrarProduto>();
builder.Services.AddScoped<AtualizarEstoque>();
builder.Services.AddScoped<EstornarEstoque>();
builder.Services.AddScoped<ObterProdutos>();
builder.Services.AddScoped<ObterProdutoPorCodigo>();
builder.Services.AddHttpClient<IIaVisionService, IaVisionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
//app.UseHttpsRedirection();
app.UseStaticFiles();

app.MapHealthChecks("/health");
app.ProdutoEndpointsMap();

app.Run();
