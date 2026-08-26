#!/bin/bash

echo -e "\n=================================================================="
echo -e "🛡️  SUÍTE DE TESTES DE RESILIÊNCIA E TOLERÂNCIA A FALHAS (MICROSERVICES)"
echo -e "==================================================================\n"

ESTOQUE_URL="http://localhost:5032/api/produtos"
FATURAMENTO_URL="http://localhost:5168/api/faturamento"

echo -e "=================================================================="
echo -e "🔬 [TESTE 1/3] Health Checks dos Microsserviços"
echo -e "=================================================================="

curl -s -o /dev/null -w "Estoque /health: %{http_code}\n" http://localhost:5032/health
curl -s -o /dev/null -w "Faturamento /health: %{http_code}\n" http://localhost:5168/health

echo -e "\n=================================================================="
echo -e "🔬 [TESTE 2/3] Transação Compensatória (Saga Rollback)"
echo -e "=================================================================="

RANDOM_ID=$((RANDOM % 9000 + 1000))
COD_A="RESIL-A-$RANDOM_ID"
COD_B="RESIL-B-$RANDOM_ID"

# 1. Cadastra Produto A (5 un)
curl -s -X POST "$ESTOQUE_URL" -H "Content-Type: application/json" -d "{\"codigo\":\"$COD_A\",\"descricao\":\"Produto A\",\"saldoInicial\":5}" > /dev/null
echo "1. Cadastrado Produto A ($COD_A) com Saldo = 5"

# 2. Cadastra Produto B (0 un)
curl -s -X POST "$ESTOQUE_URL" -H "Content-Type: application/json" -d "{\"codigo\":\"$COD_B\",\"descricao\":\"Produto B\",\"saldoInicial\":0}" > /dev/null
echo "2. Cadastrado Produto B ($COD_B) com Saldo = 0"

# 3. Cria Nota Fiscal com 2x A e 1x B
NOTA_JSON=$(curl -s -X POST "$FATURAMENTO_URL" -H "Content-Type: application/json" -d "{\"itens\":[{\"codigoProduto\":\"$COD_A\",\"quantidade\":2},{\"codigoProduto\":\"$COD_B\",\"quantidade\":1}]}")
NUM_NOTA=$(echo "$NOTA_JSON" | grep -o '"numero":[0-9]*' | cut -d: -f2)
echo "3. Criada Nota Fiscal Nº $NUM_NOTA (Aberta)"

# 4. Dispara impressão (deve falhar e executar estorno)
echo "4. Tentando imprimir NF Nº $NUM_NOTA..."
RESP_IMP=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$FATURAMENTO_URL/$NUM_NOTA/imprimir" -H "Content-Type: application/json")
echo "   Resposta: $RESP_IMP"

# 5. Auditoria de saldo
SALDO_A=$(curl -s "$ESTOQUE_URL/$COD_A" | grep -o '"saldo":[0-9]*' | cut -d: -f2)
STATUS_NF=$(curl -s "$FATURAMENTO_URL/$NUM_NOTA" | grep -o '"status":[0-9]*' | cut -d: -f2)
echo -e "\n   Auditoria:"
echo "   Saldo Produto A: $SALDO_A (Esperado: 5)"
echo "   Status NF: $STATUS_NF (Esperado: 0 - Aberta)"

echo -e "\n=================================================================="
echo -e "🔬 [TESTE 3/3] Recuperação Pós-Falha"
echo -e "=================================================================="

# Repõe estoque de B
curl -s -X PATCH "$ESTOQUE_URL/$COD_B/estornar-saldo" -H "Content-Type: application/json" -d '{"quantidade":10}' > /dev/null
echo "1. Reposto estoque do Produto B (+10 un)"

# Reimprime
echo "2. Reimprimindo NF Nº $NUM_NOTA..."
RESP_REIMP=$(curl -s -X POST "$FATURAMENTO_URL/$NUM_NOTA/imprimir" -H "Content-Type: application/json")
echo "   Resposta: $RESP_REIMP"

echo -e "\n=================================================================="
echo -e "🎉 TESTE DE RESILIÊNCIA FINALIZADO!"
echo -e "==================================================================\n"
