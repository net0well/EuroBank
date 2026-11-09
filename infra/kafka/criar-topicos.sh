#!/bin/sh
# Cria os tópicos com 3 partições antes de alguém publicar neles
# (se o Kafka os criasse sozinho, eles nasceriam com 1 partição).
set -eu

BOOTSTRAP="${BOOTSTRAP_SERVERS:-kafka:29092}"
TOPICOS=/opt/kafka/bin/kafka-topics.sh

for topico in \
  eurobank.contas.eventos \
  eurobank.contas.comandos \
  eurobank.contas.respostas \
  eurobank.banco-externo.comandos \
  eurobank.banco-externo.respostas \
  eurobank.transferencias.eventos \
  eurobank.cambio.comandos \
  eurobank.cambio.respostas
do
  $TOPICOS --bootstrap-server "$BOOTSTRAP" --create --if-not-exists \
    --topic "$topico" --partitions 3 --replication-factor 1
done

$TOPICOS --bootstrap-server "$BOOTSTRAP" --describe --topic 'eurobank\..*'
