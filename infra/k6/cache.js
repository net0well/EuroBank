// Comparação das estratégias de cache do Extrato: cada usuário virtual deposita na própria conta (Contas) e lê o
// extrato logo em seguida, repetindo a leitura até ele mostrar a versão nova da conta. A escrita chega ao Extrato
// pelo CDC (outbox, Debezium, Kafka) e pela projeção, que grava conforme a estratégia; a leitura vem do Redis ou do
// Postgres. Mede, para a estratégia em uso no extrato-worker (a mesma EXTRATO_CACHE_ESTRATEGIA vira a tag estrategia):
//   extrato_leitura_desatualizada  fração das leituras com versão anterior à da escrita já confirmada;
//   extrato_leitura_duracao        latência de cada GET /extratos/{id};
//   extrato_tempo_ate_atualizar    da resposta do depósito até a primeira leitura com a versão nova;
//   extrato_sem_atualizar          leituras que desistiram (a versão nova não apareceu em ESPERA_MAXIMA_S).
// Com o profile metricas, vão ao Prometheus por remote write e o dashboard "Cache do Extrato" do Grafana mostra as
// estratégias lado a lado. Uma rodada por estratégia:
//   docker compose --profile metricas up -d
//   for e in Nenhuma CacheAside WriteThrough WriteBehind; do
//     export EXTRATO_CACHE_ESTRATEGIA=$e
//     docker compose up -d --wait extrato-worker
//     docker compose --profile carga run --rm k6-cache
//   done

import http from 'k6/http';
import { check, fail, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { api } from './limite.js';

const API_CONTAS = __ENV.API_CONTAS || 'http://localhost:5100';
const API_EXTRATO = __ENV.API_EXTRATO || 'http://localhost:5200';
const CHAVE_CONTAS = { 'X-Api-Key': __ENV.API_KEY_CONTAS || 'contas-lab-key' };
const CHAVE_EXTRATO = { 'X-Api-Key': __ENV.API_KEY_EXTRATO || 'extrato-lab-key' };
const ESTRATEGIA = __ENV.ESTRATEGIA || 'WriteThrough';
const VUS = Number(__ENV.VUS || 4);
const DURACAO = __ENV.DURACAO || '1m';
const INTERVALO_LEITURA_S = Number(__ENV.INTERVALO_LEITURA_S || 0.05);
const ESPERA_MAXIMA_S = Number(__ENV.ESPERA_MAXIMA_S || 10);
const PAUSA_ENTRE_ESCRITAS_S = Number(__ENV.PAUSA_ENTRE_ESCRITAS_S || 0.5);

const desatualizada = new Rate('extrato_leitura_desatualizada');
const duracaoLeitura = new Trend('extrato_leitura_duracao', true);
const tempoAteAtualizar = new Trend('extrato_tempo_ate_atualizar', true);
const semAtualizar = new Counter('extrato_sem_atualizar');

const jsonContas = { headers: { ...CHAVE_CONTAS, 'Content-Type': 'application/json' } };

http.setResponseCallback(http.expectedStatuses(200, 201, 429));

export const options = {
  scenarios: {
    leitura_apos_escrita: { executor: 'constant-vus', vus: VUS, duration: DURACAO },
  },
  // Toda métrica sai com a estratégia: é por ela que o Grafana separa as rodadas.
  tags: { estrategia: ESTRATEGIA },
  thresholds: {
    extrato_sem_atualizar: ['count==0'],
  },
};

/** Uma conta por usuário virtual, com saldo para os depósitos não esbarrarem em nada. */
export function setup() {
  const contas = [];
  for (let i = 0; i < VUS; i++) {
    const aberta = api.post(`${API_CONTAS}/contas`, JSON.stringify({ titular: `Cache ${ESTRATEGIA} ${i + 1}`, moeda: 'EUR' }), jsonContas);
    if (aberta.status !== 201) {
      fail(`abrir conta: ${aberta.status} ${aberta.body}`);
    }
    contas.push(aberta.json().id);
  }

  // A primeira leitura de cada conta espera a projeção chegar, para o cenário medir só o caminho da escrita.
  for (const conta of contas) {
    esperarExtrato(conta, 1);
  }
  return { contas };
}

export default function ({ contas }) {
  const conta = contas[(__VU - 1) % contas.length];
  const deposito = api.post(`${API_CONTAS}/contas/${conta}/depositos`, JSON.stringify({ valor: 1 }), jsonContas);
  if (!check(deposito, { 'depósito 200': (resposta) => resposta.status === 200 })) {
    return;
  }

  const versao = deposito.json().versao;
  const escritaEm = Date.now();
  if (esperarExtrato(conta, versao)) {
    tempoAteAtualizar.add(Date.now() - escritaEm);
  } else {
    semAtualizar.add(1);
  }
  sleep(PAUSA_ENTRE_ESCRITAS_S);
}

/** Lê até o extrato mostrar a versão pedida (ou desistir em ESPERA_MAXIMA_S); cada leitura conta nas métricas. */
function esperarExtrato(conta, versao) {
  const limite = Date.now() + ESPERA_MAXIMA_S * 1000;
  while (Date.now() < limite) {
    const extrato = api.get(`${API_EXTRATO}/extratos/${conta}?tamanho=1`, {
      headers: CHAVE_EXTRATO,
      responseCallback: http.expectedStatuses(200, 404, 429),
    });
    duracaoLeitura.add(extrato.timings.duration);
    const atualizada = extrato.status === 200 && extrato.json().ultimaVersao >= versao;
    desatualizada.add(!atualizada);
    if (atualizada) {
      return true;
    }
    sleep(INTERVALO_LEITURA_S);
  }
  return false;
}
