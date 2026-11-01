#!/bin/sh
# Registra (ou atualiza) cada conector de infra/connect/conectores no Kafka Connect e espera ficar RUNNING.
# PUT /connectors/{nome}/config é idempotente: cria se não existir, atualiza se já existir.
set -eu

CONNECT_URL="${CONNECT_URL:-http://connect:8083}"
PASTA="${PASTA_CONECTORES:-/infra/conectores}"

for arquivo in "$PASTA"/*.json; do
  nome=$(basename "$arquivo" .json)
  echo "Registrando conector $nome..."
  curl -fsS -X PUT -H "Content-Type: application/json" --data @"$arquivo" \
    "$CONNECT_URL/connectors/$nome/config" > /dev/null
done

for arquivo in "$PASTA"/*.json; do
  nome=$(basename "$arquivo" .json)
  tentativa=0
  # Conector e task precisam estar RUNNING (duas ocorrências no status).
  until [ "$(curl -fsS "$CONNECT_URL/connectors/$nome/status" | grep -o '"state":"RUNNING"' | wc -l)" -ge 2 ]; do
    tentativa=$((tentativa + 1))
    if [ "$tentativa" -ge 60 ]; then
      echo "Conector $nome não ficou RUNNING:"
      curl -fsS "$CONNECT_URL/connectors/$nome/status"
      exit 1
    fi
    sleep 2
  done
  echo "Conector $nome RUNNING"
done
