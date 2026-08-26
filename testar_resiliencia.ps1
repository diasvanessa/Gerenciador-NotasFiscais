$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "SUITE DE TESTES DE RESILIENCIA E TOLERANCIA A FALHAS" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""

$estoqueUrl = "http://localhost:5032/api/produtos"
$faturamentoUrl = "http://localhost:5168/api/faturamento"
$estoqueHealthUrl = "http://localhost:5032/health"
$faturamentoHealthUrl = "http://localhost:5168/health"
$faturamentoDiagUrl = "http://localhost:5168/api/faturamento/status-dependencias"

# ---------------------------------------------------------------------------------
# 1. TESTE DE HEALTH CHECKS
# ---------------------------------------------------------------------------------
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host "[TESTE 1/3] Verificacao de Health Checks e Conectividade" -ForegroundColor Yellow
Write-Host "==================================================================" -ForegroundColor Yellow

try {
    $resHealthEstoque = Invoke-WebRequest -Uri $estoqueHealthUrl -Method Get -TimeoutSec 3 -UseBasicParsing
    Write-Host "   Estoque (/health): $($resHealthEstoque.StatusCode) OK" -ForegroundColor Green
} catch {
    Write-Host "   Estoque (/health): OFFLINE / Inacessivel" -ForegroundColor Red
}

try {
    $resHealthFat = Invoke-WebRequest -Uri $faturamentoHealthUrl -Method Get -TimeoutSec 3 -UseBasicParsing
    Write-Host "   Faturamento (/health): $($resHealthFat.StatusCode) OK" -ForegroundColor Green
} catch {
    Write-Host "   Faturamento (/health): OFFLINE / Inacessivel" -ForegroundColor Red
}

try {
    $resDiag = Invoke-RestMethod -Uri $faturamentoDiagUrl -Method Get -TimeoutSec 3 -UseBasicParsing
    Write-Host "   Diagnostico de Dependencias (Faturamento -> Estoque):" -ForegroundColor Cyan
    Write-Host "      Status Estoque: $($resDiag.estoque.status) | Conectado: $($resDiag.estoque.online)" -ForegroundColor White
} catch {
    Write-Host "   Nao foi possivel obter o diagnostico de dependencias." -ForegroundColor DarkGray
}
Write-Host ""

# ---------------------------------------------------------------------------------
# 2. TESTE DE TRANSAÇÃO COMPENSATÓRIA (SAGA ROLLBACK COM MÚLTIPLOS ITENS)
# ---------------------------------------------------------------------------------
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host "[TESTE 2/3] Transacao Compensatoria (Saga Rollback em Falha Parcial)" -ForegroundColor Yellow
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host "Cenario: NF com 2 itens. Item 1 tem saldo. Item 2 NAO tem saldo." -ForegroundColor White
Write-Host "Objetivo: Garantir que o Item 1 seja estornado e a NF permaneca Aberta." -ForegroundColor White
Write-Host ""

$codProdA = "RESIL-A-" + (Get-Random -Minimum 1000 -Maximum 9999)
$codProdB = "RESIL-B-" + (Get-Random -Minimum 1000 -Maximum 9999)

# 2.1 Cadastrar Produto A (Saldo = 5)
$bodyProdA = @{ codigo = $codProdA; descricao = "Produto A (Com Saldo)"; saldoInicial = 5 } | ConvertTo-Json
$resProdA = Invoke-RestMethod -Uri $estoqueUrl -Method Post -Body $bodyProdA -ContentType "application/json" -UseBasicParsing
Write-Host "   1. Cadastrado Produto A ('$codProdA') com Saldo = 5 un" -ForegroundColor Gray

# 2.2 Cadastrar Produto B (Saldo = 0)
$bodyProdB = @{ codigo = $codProdB; descricao = "Produto B (Sem Saldo)"; saldoInicial = 0 } | ConvertTo-Json
$resProdB = Invoke-RestMethod -Uri $estoqueUrl -Method Post -Body $bodyProdB -ContentType "application/json" -UseBasicParsing
Write-Host "   2. Cadastrado Produto B ('$codProdB') com Saldo = 0 un" -ForegroundColor Gray

# 2.3 Criar Nota Fiscal com 2 un de A e 1 un de B
$bodyNota = @{
    itens = @(
        @{ codigoProduto = $codProdA; quantidade = 2 },
        @{ codigoProduto = $codProdB; quantidade = 1 }
    )
} | ConvertTo-Json

$resNota = Invoke-RestMethod -Uri $faturamentoUrl -Method Post -Body $bodyNota -ContentType "application/json" -UseBasicParsing
$numNota = [int]$resNota.numero
Write-Host "   3. Criada Nota Fiscal No $($numNota) (Aberta) com 2x '$codProdA' e 1x '$codProdB'" -ForegroundColor Gray

# 2.4 Tentar Imprimir a Nota (Esperado: Falha no item B -> Rollback no item A)
Write-Host "   4. Disparando tentativa de impressao da NF No $($numNota)..." -ForegroundColor Magenta

$respostaImpressao = $null
try {
    $resImpOk = Invoke-RestMethod -Uri "$faturamentoUrl/$numNota/imprimir" -Method Post -ContentType "application/json" -UseBasicParsing
    $respostaImpressao = [PSCustomObject]@{ Status = 200; Corpo = ($resImpOk | ConvertTo-Json) }
} catch {
    $statusCode = [int]$_.Exception.Response.StatusCode
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $corpoErro = $reader.ReadToEnd()
    $respostaImpressao = [PSCustomObject]@{ Status = $statusCode; Corpo = $corpoErro }
}

Write-Host "   5. Resposta da API de Faturamento:" -ForegroundColor Cyan
Write-Host "      HTTP Status: $($respostaImpressao.Status)" -ForegroundColor White
Write-Host "      Payload: $($respostaImpressao.Corpo)" -ForegroundColor Gray

# 2.5 Verificar se o saldo do Produto A foi restaurado (Compensado)
$prodAFinal = Invoke-RestMethod -Uri "$estoqueUrl/$codProdA" -Method Get -UseBasicParsing
$notaFinal = Invoke-RestMethod -Uri "$faturamentoUrl/$numNota" -Method Get -UseBasicParsing

Write-Host ""
Write-Host "   Auditoria Pos-Falha:" -ForegroundColor Yellow
Write-Host "      Saldo Final do Produto A: $($prodAFinal.saldo) un (Esperado: 5 un)" -ForegroundColor White
Write-Host "      Status da Nota Fiscal No $($numNota): $($notaFinal.status) (Esperado: 0 = Aberta)" -ForegroundColor White

if ($prodAFinal.saldo -eq 5 -and $notaFinal.status -eq 0) {
    Write-Host ""
    Write-Host "   [SUCESSO] Transacao Compensatoria (Saga Rollback) funcionou perfeitamente!" -ForegroundColor Green
    Write-Host "   Nenhum saldo foi perdido e a NF nao foi corrompida." -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "   [ERRO] Saldo ou status da NF inconsistente!" -ForegroundColor Red
    Write-Host ""
}

# ---------------------------------------------------------------------------------
# 3. TESTE DE RECUPERAÇÃO PÓS-FALHA (SELF-HEALING / REPROCESSAMENTO SEGURO)
# ---------------------------------------------------------------------------------
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host "[TESTE 3/3] Recuperacao e Reprocessamento com Sucesso" -ForegroundColor Yellow
Write-Host "==================================================================" -ForegroundColor Yellow
Write-Host "Cenario: O operador repoe o estoque do Produto B e clica em Imprimir novamente." -ForegroundColor White
Write-Host ""

# 3.1 Adicionar saldo ao Produto B
$bodySaldoB = @{ quantidade = 10 } | ConvertTo-Json
$resEstorno = Invoke-RestMethod -Uri "$estoqueUrl/$codProdB/estornar-saldo" -Method Patch -Body $bodySaldoB -ContentType "application/json" -UseBasicParsing
Write-Host "   1. Reposto estoque do Produto B ('$codProdB'): +10 un (Saldo atual = 10)" -ForegroundColor Gray

# 3.2 Reprocessar a impressão da mesma Nota Fiscal Nº $numNota
Write-Host "   2. Reprocessando impressao da NF No $($numNota)..." -ForegroundColor Magenta
$resReimprimir = Invoke-RestMethod -Uri "$faturamentoUrl/$numNota/imprimir" -Method Post -ContentType "application/json" -UseBasicParsing

# 3.3 Validar status da NF e saldos
$notaRecuperada = Invoke-RestMethod -Uri "$faturamentoUrl/$numNota" -Method Get -UseBasicParsing
$prodARecuperado = Invoke-RestMethod -Uri "$estoqueUrl/$codProdA" -Method Get -UseBasicParsing
$prodBRecuperado = Invoke-RestMethod -Uri "$estoqueUrl/$codProdB" -Method Get -UseBasicParsing

Write-Host "   3. Resultado da Impressao:" -ForegroundColor Cyan
Write-Host "      Mensagem: $($resReimprimir.mensagem)" -ForegroundColor Green
Write-Host "      Status da NF: $($notaRecuperada.status) (Esperado: 1 = Fechada)" -ForegroundColor White
Write-Host "      Saldo Produto A: $($prodARecuperado.saldo) un (Esperado: 3 un)" -ForegroundColor White
Write-Host "      Saldo Produto B: $($prodBRecuperado.saldo) un (Esperado: 9 un)" -ForegroundColor White

if ($notaRecuperada.status -eq 1 -and $prodARecuperado.saldo -eq 3 -and $prodBRecuperado.saldo -eq 9) {
    Write-Host ""
    Write-Host "   [SUCESSO] Recuperacao da falha concluida com exito e integridade total dos dados!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "   [ERRO] Falha na recuperacao!" -ForegroundColor Red
    Write-Host ""
}

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "RESUMO DOS TESTES DE RESILIENCIA CONCLUIDO COM SUCESSO!" -ForegroundColor Cyan
Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host ""
