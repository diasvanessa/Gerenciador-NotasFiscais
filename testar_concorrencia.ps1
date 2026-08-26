$ErrorActionPreference = "Continue"

Write-Host "`n==================================================================" -ForegroundColor Cyan
Write-Host "🧪 TESTE DE CONCORRÊNCIA: 2 NOTAS FISCAIS PARA 1 UNIDADE DE SALDO" -ForegroundColor Cyan
Write-Host "==================================================================`n" -ForegroundColor Cyan

$estoqueUrl = "http://localhost:5032/api/produtos"
$faturamentoUrl = "http://localhost:5168/api/faturamento"

# 1. Cadastrar produto de teste com Saldo = 1
$codigo = "CONC-" + (Get-Random -Minimum 1000 -Maximum 9999)
Write-Host "[1/4] Cadastrando produto '$codigo' com Saldo Inicial = 1 no Estoque..." -ForegroundColor Yellow

$bodyProd = @{
    codigo = $codigo
    descricao = "Produto Teste Concorrencia"
    saldoInicial = 1
} | ConvertTo-Json

$resProd = Invoke-RestMethod -Uri $estoqueUrl -Method Post -Body $bodyProd -ContentType "application/json"
Write-Host "      ✅ Produto cadastrado com sucesso! Saldo atual: $($resProd.saldo) un`n" -ForegroundColor Green

# 2. Emitir Nota Fiscal A
Write-Host "[2/4] Criando Nota Fiscal 1 (NF-A)..." -ForegroundColor Yellow
$bodyNota1 = @{
    itens = @(
        @{ codigoProduto = $codigo; quantidade = 1 }
    )
} | ConvertTo-Json

$resNota1 = Invoke-RestMethod -Uri $faturamentoUrl -Method Post -Body $bodyNota1 -ContentType "application/json"
$numNota1 = $resNota1.numero
Write-Host "      ✅ NF-A criada: Nº $numNota1 (Status: Aberta)`n" -ForegroundColor Green

# 3. Emitir Nota Fiscal B
Write-Host "[3/4] Criando Nota Fiscal 2 (NF-B)..." -ForegroundColor Yellow
$bodyNota2 = @{
    itens = @(
        @{ codigoProduto = $codigo; quantidade = 1 }
    )
} | ConvertTo-Json

$resNota2 = Invoke-RestMethod -Uri $faturamentoUrl -Method Post -Body $bodyNota2 -ContentType "application/json"
$numNota2 = $resNota2.numero
Write-Host "      ✅ NF-B criada: Nº $numNota2 (Status: Aberta)`n" -ForegroundColor Green

# 4. Disparar a impressão simultânea das duas notas em paralelo
Write-Host "[4/4] 🔥 DISPARANDO IMPRESSÃO SIMULTÂNEA DAS DUAS NOTAS FISCAIS..." -ForegroundColor Magenta

$url1 = "$faturamentoUrl/$numNota1/imprimir"
$url2 = "$faturamentoUrl/$numNota2/imprimir"

$job1 = Start-Job -ScriptBlock {
    param($url)
    try {
        $res = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json"
        return [PSCustomObject]@{ Status = 200; Mensagem = $res.mensagem; Sucesso = $true }
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        $statusCode = [int]$_.Exception.Response.StatusCode
        return [PSCustomObject]@{ Status = $statusCode; Mensagem = $body; Sucesso = $false }
    }
} -ArgumentList $url1

$job2 = Start-Job -ScriptBlock {
    param($url)
    try {
        $res = Invoke-RestMethod -Uri $url -Method Post -ContentType "application/json"
        return [PSCustomObject]@{ Status = 200; Mensagem = $res.mensagem; Sucesso = $true }
    } catch {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
        $statusCode = [int]$_.Exception.Response.StatusCode
        return [PSCustomObject]@{ Status = $statusCode; Mensagem = $body; Sucesso = $false }
    }
} -ArgumentList $url2

# Aguarda ambas as requisições terminarem
$result1 = Receive-Job -Job $job1 -Wait
$result2 = Receive-Job -Job $job2 -Wait
Remove-Job -Job $job1, $job2

Write-Host "`n===================== RESULTADO DA CONCORRÊNCIA =====================" -ForegroundColor Cyan

Write-Host "`n👉 Nota Fiscal Nº $numNota1:" -ForegroundColor White
if ($result1.Sucesso) {
    Write-Host "   Status: HTTP $($result1.Status) (SUCESSO - Fechada)" -ForegroundColor Green
    Write-Host "   Resposta: $($result1.Mensagem)" -ForegroundColor Gray
} else {
    Write-Host "   Status: HTTP $($result1.Status) (BLOQUEADA POR CONCORRÊNCIA / SEM SALDO)" -ForegroundColor Red
    Write-Host "   Resposta: $($result1.Mensagem)" -ForegroundColor Gray
}

Write-Host "`n👉 Nota Fiscal Nº $numNota2:" -ForegroundColor White
if ($result2.Sucesso) {
    Write-Host "   Status: HTTP $($result2.Status) (SUCESSO - Fechada)" -ForegroundColor Green
    Write-Host "   Resposta: $($result2.Mensagem)" -ForegroundColor Gray
} else {
    Write-Host "   Status: HTTP $($result2.Status) (BLOQUEADA POR CONCORRÊNCIA / SEM SALDO)" -ForegroundColor Red
    Write-Host "   Resposta: $($result2.Mensagem)" -ForegroundColor Gray
}

# 5. Conferir saldo final no estoque
$prodFinal = Invoke-RestMethod -Uri "$estoqueUrl/$codigo" -Method Get
Write-Host "`n📦 SALDO FINAL NO ESTOQUE:" -ForegroundColor Yellow
Write-Host "   Produto: $codigo" -ForegroundColor White
Write-Host "   Saldo Restante: $($prodFinal.saldo) un" -ForegroundColor Cyan

if ($prodFinal.saldo -eq 0 -and (($result1.Sucesso -and -not $result2.Sucesso) -or ($result2.Sucesso -and -not $result1.Sucesso))) {
    Write-Host "`n🎉 TESTE PASSOU COM PERFEIÇÃO! Apenas uma nota conseguiu fechar, e o estoque nunca ficou negativo!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️ Verifique o comportamento de concorrência." -ForegroundColor Yellow
}
Write-Host "=====================================================================`n" -ForegroundColor Cyan
