#!/usr/bin/env bash

echo "================================================="
echo "🧹 LIMPANDO AS TABELAS DOS BANCOS SQLITE..."
echo "================================================="

# Opção de reset limpo dos arquivos sqlite
rm -f Estoque/Estoque.sqlite*
rm -f Faturamento/Faturamento.sqlite*

echo -e "\n📦 Recriando estrutura do banco Estoque..."
cd Estoque && dotnet ef database update && cd ..

echo -e "\n📄 Recriando estrutura do banco Faturamento..."
cd Faturamento && dotnet ef database update && cd ..

echo -e "\n✅ Todas as tabelas foram limpas e recriadas com sucesso!"
echo "================================================="
