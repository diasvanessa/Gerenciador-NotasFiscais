#!/usr/bin/env bash

echo "=================================================================="
echo "🧪 TESTE DE CONCORRÊNCIA: 2 NOTAS FISCAIS PARA 1 UNIDADE DE SALDO"
echo "=================================================================="

ESTOQUE_URL="http://localhost:5032/api/produtos"
FATURAMENTO_URL="http://localhost:5168/api/faturamento"

# 1. Cadastra produto com Saldo = 1
CODIGO="CONC-$RANDOM"
echo -e "\n[1/4] Cadastrando produto '$CODIGO' com Saldo Inicial = 1..."
curl -s -X POST "$ESTOQUE_URL" \
  -H "Content-Type: application/json" \
  -d "{\"codigo\": \"$CODIGO\", \"descricao\": \"Produto Concorrencia\", \"saldoInicial\": 1}"

# 2. Cria Nota Fiscal A
echo -e "\n\n[2/4] Criando Nota Fiscal 1 (NF-A)..."
RES_NOTA1=$(curl -s -X POST "$FATURAMENTO_URL" \
  -H "Content-Type: application/json" \
  -d "{\"itens\": [{\"codigoProduto\": \"$CODIGO\", \"quantidade\": 1}]}")
NUM_NOTA1=$(echo "$RES_NOTA1" | grep -o '"numero":[0-9]*' | cut -d':' -f2)
echo "NF-A criada: Nº $NUM_NOTA1"

# 3. Cria Nota Fiscal B
echo -e "\n[3/4] Criando Nota Fiscal 2 (NF-B)..."
RES_NOTA2=$(curl -s -X POST "$FATURAMENTO_URL" \
  -H "Content-Type: application/json" \
  -d "{\"itens\": [{\"codigoProduto\": \"$CODIGO\", \"quantidade\": 1}]}")
NUM_NOTA2=$(echo "$RES_NOTA2" | grep -o '"numero":[0-9]*' | cut -d':' -f2)
echo "NF-B criada: Nº $NUM_NOTA2"

# 4. Dispara impressão simultânea em background com curl em paralelo
echo -e "\n[4/4] 🔥 Disparando IMPRESSÃO SIMULTÂNEA (NF $NUM_NOTA1 e NF $NUM_NOTA2)..."

TMP_RES1=$(mktemp)
TMP_RES2=$(mktemp)

curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$FATURAMENTO_URL/$NUM_NOTA1/imprimir" > "$TMP_RES1" &
PID1=$!

curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST "$FATURAMENTO_URL/$NUM_NOTA2/imprimir" > "$TMP_RES2" &
PID2=$!

wait $PID1 $PID2

echo -e "\n===================== RESULTADO DA CONCORRÊNCIA ====================="
echo "👉 Resposta da NF Nº $NUM_NOTA1:"
cat "$TMP_RES1"

echo -e "\n\n👉 Resposta da NF Nº $NUM_NOTA2:"
cat "$TMP_RES2"

rm -f "$TMP_RES1" "$TMP_RES2"

echo -e "\n\n📦 SALDO FINAL NO ESTOQUE:"
curl -s "$ESTOQUE_URL/$CODIGO"
echo -e "\n=====================================================================\n"
