#!/bin/sh
# Cria os databases do laboratório que ainda não existem. Roda a cada "up" (é idempotente), então também
# serve para um volume antigo do Postgres, em que os scripts de init da imagem não rodam mais.
# O eurobank_contas é criado pela própria imagem (POSTGRES_DB).
set -eu

export PGPASSWORD="$POSTGRES_PASSWORD"

for banco in eurobank_extrato eurobank_transferencias eurobank_banco_externo; do
  existe=$(psql -h postgres -U "$POSTGRES_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$banco'")
  if [ "$existe" = "1" ]; then
    echo "Database $banco já existe"
  else
    psql -h postgres -U "$POSTGRES_USER" -d postgres -c "CREATE DATABASE $banco"
    echo "Database $banco criado"
  fi
done
