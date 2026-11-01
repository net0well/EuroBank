#!/bin/sh
# Cria o tópico da outbox com 3 partições antes de o Debezium publicar nele
# (se o Kafka o criasse sozinho, ele nasceria com 1 partição).
set -eu

BOOTSTRAP="${BOOTSTRAP_SERVERS:-kafka:29092}"
TOPICOS=/opt/kafka/bin/kafka-topics.sh

$TOPICOS --bootstrap-server "$BOOTSTRAP" --create --if-not-exists \
  --topic eurobank.contas.eventos --partitions 3 --replication-factor 1

$TOPICOS --bootstrap-server "$BOOTSTRAP" --describe --topic eurobank.contas.eventos
