using System.Net.Http.Json;
using System.Text.Json.Nodes;

var client = new HttpClient();

var estoqueUrl = "http://localhost:5032/api/produtos";
var faturamentoUrl = "http://localhost:5168/api/faturamento";

Console.WriteLine("==================================================================");
Console.WriteLine("🧪 TESTE DE CONCORRÊNCIA DUPLA: 2 NOTAS FISCAIS PARA 1 UN DE SALDO");
Console.WriteLine("==================================================================\n");

// 1. Cadastra um produto com saldo inicial = 1
var codigoProduto = $"TESTE-{Guid.NewGuid().ToString()[..6].ToUpper()}";
Console.WriteLine($"[1/4] Cadastrando produto '{codigoProduto}' com Saldo Inicial = 1...");

var novoProduto = new { codigo = codigoProduto, descricao = "Produto Concorrência", saldoInicial = 1 };
var resCadProduto = await client.PostAsJsonAsync(estoqueUrl, novoProduto);
Console.WriteLine($"Status do Cadastro: {resCadProduto.StatusCode}\n");

// 2. Cria a Nota Fiscal 1 (NF-A) com 1 un do produto
Console.WriteLine($"[2/4] Emitindo Nota Fiscal 1 (NF-A)...");
var reqNota1 = new { itens = new[] { new { codigoProduto, quantidade = 1 } } };
var resNota1 = await client.PostAsJsonAsync(faturamentoUrl, reqNota1);
var jsonNota1 = await resNota1.Content.ReadAsStringAsync();
var numNota1 = JsonNode.Parse(jsonNota1)?["numero"]?.GetValue<int>() ?? 0;
Console.WriteLine($"NF-A criada com sucesso: Nº {numNota1} (Aberta)\n");

// 3. Cria a Nota Fiscal 2 (NF-B) com 1 un do mesmo produto
Console.WriteLine($"[3/4] Emitindo Nota Fiscal 2 (NF-B)...");
var reqNota2 = new { itens = new[] { new { codigoProduto, quantidade = 1 } } };
var resNota2 = await client.PostAsJsonAsync(faturamentoUrl, reqNota2);
var jsonNota2 = await resNota2.Content.ReadAsStringAsync();
var numNota2 = JsonNode.Parse(jsonNota2)?["numero"]?.GetValue<int>() ?? 0;
Console.WriteLine($"NF-B criada com sucesso: Nº {numNota2} (Aberta)\n");

// 4. Dispara a impressão simultânea das duas notas em paralelo (Task.WhenAll)
Console.WriteLine($"[4/4] 🔥 Disparando IMPRESSÃO SIMULTÂNEA da NF {numNota1} e NF {numNota2}...");

var task1 = client.PostAsync($"{faturamentoUrl}/{numNota1}/imprimir", null);
var task2 = client.PostAsync($"{faturamentoUrl}/{numNota2}/imprimir", null);

var responses = await Task.WhenAll(task1, task2);

Console.WriteLine("\n===================== RESULTADOS OBTIDOS =====================");
for (int i = 0; i < responses.Length; i++)
{
    var res = responses[i];
    var body = await res.Content.ReadAsStringAsync();
    var notaNum = (i == 0) ? numNota1 : numNota2;
    Console.WriteLine($"👉 Nota Fiscal Nº {notaNum}: HTTP {(int)res.StatusCode} ({res.StatusCode})");
    Console.WriteLine($"   Retorno da API: {body}\n");
}

// 5. Verifica o saldo final no Estoque
var resEstoqueFinal = await client.GetAsync($"{estoqueUrl}/{codigoProduto}");
var bodyEstoque = await resEstoqueFinal.Content.ReadAsStringAsync();
Console.WriteLine($"📦 Consulta Final de Estoque do Produto '{codigoProduto}':");
Console.WriteLine($"   {bodyEstoque}");
Console.WriteLine("==============================================================");