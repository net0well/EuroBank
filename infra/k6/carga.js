// Gerador de carga do EuroBank.
//   docker compose --profile carga run --rm k6
//   K6_VUS=20 K6_DURACAO=2m docker compose --profile carga run --rm k6
//
// 1. setup:    abre CONTAS contas com depósito inicial.
// 2. default:  cada usuário virtual dispara operações aleatórias (depósito, saque, transferência).
//              422 (regra de negócio) e 409 (concorrência) são respostas esperadas sob carga.
// 3. teardown: espera o Extrato alcançar Contas e confere, conta a conta, saldo e versão.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const API_CONTAS = __ENV.API_CONTAS || 'http://localhost:5100';
const API_EXTRATO = __ENV.API_EXTRATO || 'http://localhost:5200';
const CONTAS = Number(__ENV.CONTAS || 10);
const ESPERA_CONVERGENCIA_S = Number(__ENV.ESPERA_CONVERGENCIA_S || 90);
const CHAVE_CONTAS = { 'X-Api-Key': __ENV.API_KEY_CONTAS || 'contas-lab-key' };
const CHAVE_EXTRATO = { 'X-Api-Key': __ENV.API_KEY_EXTRATO || 'extrato-lab-key' };

const divergencias = new Counter('contas_divergentes');

http.setResponseCallback(http.expectedStatuses(200, 201, 409, 422));

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURACAO || '1m',
  teardownTimeout: `${ESPERA_CONVERGENCIA_S + 30}s`,
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'checks{etapa:operacao}': ['rate>0.99'],
    contas_divergentes: ['count==0'],
  },
};

const json = { headers: { 'Content-Type': 'application/json', ...CHAVE_CONTAS } };

function valorAleatorio(maximo) {
  return Math.round((1 + Math.random() * (maximo - 1)) * 100) / 100;
}

export function setup() {
  const contas = [];
  for (let i = 0; i < CONTAS; i++) {
    const aberta = http.post(`${API_CONTAS}/contas`, JSON.stringify({ titular: `Carga ${i + 1}` }), json);
    const conta = aberta.json();
    http.post(`${API_CONTAS}/contas/${conta.id}/depositos`, JSON.stringify({ valor: 1000 }), json);
    contas.push({ id: conta.id, iban: conta.iban });
  }
  return { contas };
}

export default function (dados) {
  const contas = dados.contas;
  const origem = contas[Math.floor(Math.random() * contas.length)];
  const sorteio = Math.random();
  let resposta;

  if (sorteio < 0.5) {
    resposta = http.post(`${API_CONTAS}/contas/${origem.id}/depositos`,
      JSON.stringify({ valor: valorAleatorio(100) }), json);
  } else if (sorteio < 0.8) {
    resposta = http.post(`${API_CONTAS}/contas/${origem.id}/saques`,
      JSON.stringify({ valor: valorAleatorio(150) }), json);
  } else {
    let destino = contas[Math.floor(Math.random() * contas.length)];
    if (destino.id === origem.id) {
      destino = contas[(contas.indexOf(origem) + 1) % contas.length];
    }
    resposta = http.post(`${API_CONTAS}/contas/${origem.id}/transferencias`,
      JSON.stringify({ ibanDestino: destino.iban, valor: valorAleatorio(200) }), json);
  }

  check(resposta, { 'operação respondeu 200, 409 ou 422': (r) => [200, 409, 422].includes(r.status) },
    { etapa: 'operacao' });
  sleep(0.05);
}

export function teardown(dados) {
  const limite = Date.now() + ESPERA_CONVERGENCIA_S * 1000;
  let pendentes = dados.contas;

  while (pendentes.length > 0 && Date.now() < limite) {
    pendentes = pendentes.filter((conta) => !convergiu(conta));
    if (pendentes.length > 0) {
      sleep(2);
    }
  }

  for (const conta of pendentes) {
    const contas = http.get(`${API_CONTAS}/contas/${conta.id}`, { headers: CHAVE_CONTAS }).json();
    const extrato = http.get(`${API_EXTRATO}/extratos/${conta.id}?tamanho=1`, { headers: CHAVE_EXTRATO }).json();
    console.error(`Divergente ${conta.id}: Contas saldo=${contas.saldo} v${contas.versao}; ` +
      `Extrato saldo=${extrato.saldoAtual} v${extrato.ultimaVersao}`);
    divergencias.add(1);
  }

  check(pendentes, { 'todas as contas convergiram no Extrato': (p) => p.length === 0 }, { etapa: 'convergencia' });
  console.log(`Convergência: ${dados.contas.length - pendentes.length}/${dados.contas.length} contas iguais em Contas e Extrato.`);
}

function convergiu(conta) {
  const contas = http.get(`${API_CONTAS}/contas/${conta.id}`, { headers: CHAVE_CONTAS });
  const extrato = http.get(`${API_EXTRATO}/extratos/${conta.id}?tamanho=1`, { headers: CHAVE_EXTRATO, responseCallback: http.expectedStatuses(200, 404) });
  if (contas.status !== 200 || extrato.status !== 200) {
    return false;
  }
  const c = contas.json();
  const e = extrato.json();
  return c.versao === e.ultimaVersao && Number(c.saldo) === Number(e.saldoAtual);
}
