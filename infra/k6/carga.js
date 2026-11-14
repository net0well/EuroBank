// Gerador de carga do EuroBank: operações de Contas misturadas a sagas de Transferencias.
//   SEPA_HORARIO_DE_CORTE=false docker compose up -d transferencias-api
//   docker compose --profile carga run --rm k6
//   K6_VUS=20 K6_DURACAO=2m docker compose --profile carga run --rm k6
//   K6_FINAIS_LENTOS=true docker compose --profile carga run --rm k6     (inclui os finais 98 e 95: ~7 min de espera)
//
// 1. setup:    abre CONTAS contas em EUR, USD, GBP e BRL com depósito inicial.
// 2. default:  cada usuário virtual dispara operações aleatórias: depósito, saque, transferência interna (mesma
//              moeda), transferência externa (finais 00, 99, 97, 96 e 94 do IBAN; 98 e 95 com K6_FINAIS_LENTOS) e
//              câmbio interno. 422 (regra de negócio) e 409 (concorrência) são respostas esperadas sob carga.
// 3. teardown: espera todas as sagas chegarem ao estado final, espera o Extrato alcançar Contas (saldo e versão,
//              conta a conta) e confere, moeda a moeda, que nenhum dinheiro sumiu:
//                soma dos saldos = depósitos - saques - saídas das sagas + créditos de câmbio recebidos.
//
// O horário de corte SEPA precisa estar desligado: depois das 16:00 UTC ou no fim de semana, as externas em EUR
// ficariam Agendadas para o próximo dia útil e a carga nunca terminaria. O k6 falha se alguma vier Agendada.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

const API_CONTAS = __ENV.API_CONTAS || 'http://localhost:5100';
const API_EXTRATO = __ENV.API_EXTRATO || 'http://localhost:5200';
const API_TRANSFERENCIAS = __ENV.API_TRANSFERENCIAS || 'http://localhost:5300';
const CONTAS = Number(__ENV.CONTAS || 10);
const FINAIS_LENTOS = (__ENV.FINAIS_LENTOS || 'false').toLowerCase() === 'true';
const ESPERA_SAGAS_S = Number(__ENV.ESPERA_SAGAS_S || (FINAIS_LENTOS ? 420 : 180));
const ESPERA_CONVERGENCIA_S = Number(__ENV.ESPERA_CONVERGENCIA_S || 90);
const ESPERA_PROJECAO_S = Number(__ENV.ESPERA_PROJECAO_S || 5);
const CHAVE_CONTAS = { 'X-Api-Key': __ENV.API_KEY_CONTAS || 'contas-lab-key' };
const CHAVE_EXTRATO = { 'X-Api-Key': __ENV.API_KEY_EXTRATO || 'extrato-lab-key' };
const CHAVE_TRANSFERENCIAS = { 'X-Api-Key': __ENV.API_KEY_TRANSFERENCIAS || 'transferencias-lab-key' };

const MOEDAS = ['EUR', 'USD', 'GBP', 'BRL'];
// Escala dos valores por moeda, para as operações terem tamanhos parecidos em todas.
const ESCALA = { EUR: 1, USD: 1.1, GBP: 0.9, BRL: 6 };
const IBAN_EXTERNO = 'DE893704004405320130';
const FINAIS = FINAIS_LENTOS
  ? [['00', 50], ['99', 10], ['97', 10], ['96', 10], ['94', 10], ['98', 5], ['95', 5]]
  : [['00', 60], ['99', 10], ['97', 10], ['96', 10], ['94', 10]];
const ESTADOS_FINAIS = ['Concluida', 'Cancelada', 'Recusada', 'Devolvida', 'Divergente'];

const divergencias = new Counter('contas_divergentes');
const sagasPendentes = new Counter('sagas_pendentes');
const transferenciasAgendadas = new Counter('transferencias_agendadas');
const moedasQueNaoFecham = new Counter('moedas_que_nao_fecham');
const sagasIniciadas = new Counter('sagas_iniciadas');

http.setResponseCallback(http.expectedStatuses(200, 201, 202, 409, 422));

export const options = {
  vus: Number(__ENV.VUS || 10),
  duration: __ENV.DURACAO || '1m',
  setupTimeout: '2m',
  teardownTimeout: `${ESPERA_SAGAS_S + ESPERA_CONVERGENCIA_S + 120}s`,
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'checks{etapa:operacao}': ['rate>0.99'],
    contas_divergentes: ['count==0'],
    sagas_pendentes: ['count==0'],
    transferencias_agendadas: ['count==0'],
    moedas_que_nao_fecham: ['count==0'],
  },
};

const jsonContas = { headers: { 'Content-Type': 'application/json', ...CHAVE_CONTAS } };

function valorAleatorio(maximo, moeda) {
  const teto = maximo * ESCALA[moeda];
  return Math.round((1 + Math.random() * (teto - 1)) * 100) / 100;
}

function sortear(lista) {
  return lista[Math.floor(Math.random() * lista.length)];
}

function sortearPonderado(pesos) {
  const total = pesos.reduce((soma, [, peso]) => soma + peso, 0);
  let sorteio = Math.random() * total;
  for (const [valor, peso] of pesos) {
    sorteio -= peso;
    if (sorteio < 0) {
      return valor;
    }
  }
  return pesos[0][0];
}

function centavos(valor) {
  return Math.round(Number(valor || 0) * 100);
}

function emReais(totalEmCentavos) {
  return (totalEmCentavos / 100).toFixed(2);
}

export function setup() {
  const contas = [];
  for (let i = 0; i < CONTAS; i++) {
    const moeda = MOEDAS[i % MOEDAS.length];
    const aberta = http.post(`${API_CONTAS}/contas`, JSON.stringify({ titular: `Carga ${i + 1}`, moeda }), jsonContas);
    const conta = aberta.json();
    http.post(`${API_CONTAS}/contas/${conta.id}/depositos`, JSON.stringify({ valor: 1000 * ESCALA[moeda] }), jsonContas);
    contas.push({ id: conta.id, iban: conta.iban, moeda });
  }

  sondarTransferencias(contas.find((conta) => conta.moeda === 'EUR') || contas[0]);

  // Transferencias conhece as contas pela projeção de eurobank.contas.eventos: dá tempo de ela alcançar as demais.
  sleep(ESPERA_PROJECAO_S);
  return { contas };
}

// Uma externa de verdade (entra no balanço como qualquer outra). Repete enquanto a conta não chegou à projeção de
// Transferencias; se vier Agendada, o corte SEPA está ligado: cancela a sonda e aborta antes de gerar carga.
function sondarTransferencias(conta) {
  const chave = crypto.randomUUID();
  const fim = Date.now() + 60 * 1000;
  let resposta;
  do {
    resposta = http.post(`${API_TRANSFERENCIAS}/transferencias-externas`, JSON.stringify({
      contaOrigemId: conta.id,
      ibanDestino: `${IBAN_EXTERNO}00`,
      bic: 'DEUTDEFF',
      nomeBeneficiario: 'Sonda da carga',
      valor: 1,
      moedaDoValor: conta.moeda,
      moedaDestino: conta.moeda,
    }), { headers: { 'Content-Type': 'application/json', 'Idempotency-Key': chave, ...CHAVE_TRANSFERENCIAS } });
    if (resposta.status === 422 && resposta.body.includes('conta_origem_desconhecida')) {
      sleep(1);
    }
  } while (resposta.status === 422 && resposta.body.includes('conta_origem_desconhecida') && Date.now() < fim);

  if (resposta.status !== 202) {
    throw new Error(`A sonda de Transferencias respondeu ${resposta.status}: ${resposta.body}`);
  }

  const sonda = resposta.json();
  if (sonda.estado === 'Agendada') {
    http.post(`${API_TRANSFERENCIAS}/transferencias/${sonda.id}/cancelamento`, null, { headers: CHAVE_TRANSFERENCIAS });
    throw new Error('O horário de corte SEPA está ligado (a sonda ficou Agendada para o próximo dia útil). Rode ' +
      '"SEPA_HORARIO_DE_CORTE=false docker compose up -d transferencias-api" e repita a carga.');
  }
}

export default function (dados) {
  const contas = dados.contas;
  const origem = sortear(contas);
  const sorteio = Math.random();
  let resposta;

  if (sorteio < 0.40) {
    resposta = http.post(`${API_CONTAS}/contas/${origem.id}/depositos`,
      JSON.stringify({ valor: valorAleatorio(100, origem.moeda) }), jsonContas);
  } else if (sorteio < 0.62) {
    resposta = http.post(`${API_CONTAS}/contas/${origem.id}/saques`,
      JSON.stringify({ valor: valorAleatorio(150, origem.moeda) }), jsonContas);
  } else if (sorteio < 0.77) {
    resposta = transferenciaInterna(contas, origem);
  } else if (sorteio < 0.93) {
    resposta = transferenciaExterna(origem);
  } else {
    resposta = cambioInterno(contas, origem);
  }

  check(resposta, { 'operação respondeu 200, 202, 409 ou 422': (r) => [200, 202, 409, 422].includes(r.status) },
    { etapa: 'operacao' });
  sleep(0.05);
}

function transferenciaInterna(contas, origem) {
  const mesmaMoeda = contas.filter((conta) => conta.moeda === origem.moeda && conta.id !== origem.id);
  if (mesmaMoeda.length === 0) {
    return http.post(`${API_CONTAS}/contas/${origem.id}/depositos`,
      JSON.stringify({ valor: valorAleatorio(100, origem.moeda) }), jsonContas);
  }
  return http.post(`${API_CONTAS}/contas/${origem.id}/transferencias`,
    JSON.stringify({ ibanDestino: sortear(mesmaMoeda).iban, valor: valorAleatorio(200, origem.moeda) }), jsonContas);
}

function transferenciaExterna(origem) {
  const final = sortearPonderado(FINAIS);
  // Um quarto das externas sai em outra moeda: a saga cota no câmbio antes de debitar.
  const moedaDestino = Math.random() < 0.25 ? sortear(MOEDAS.filter((moeda) => moeda !== origem.moeda)) : origem.moeda;
  return pedirSaga('/transferencias-externas', {
    contaOrigemId: origem.id,
    ibanDestino: `${IBAN_EXTERNO}${final}`,
    bic: 'DEUTDEFF',
    nomeBeneficiario: `Beneficiário ${final}`,
    valor: valorAleatorio(50, origem.moeda),
    moedaDoValor: origem.moeda,
    moedaDestino,
    emailNotificacao: emailDeVezEmQuando(),
  });
}

function cambioInterno(contas, origem) {
  const outrasMoedas = contas.filter((conta) => conta.moeda !== origem.moeda);
  if (outrasMoedas.length === 0) {
    return transferenciaExterna(origem);
  }
  return pedirSaga('/transferencias-cambio', {
    contaOrigemId: origem.id,
    ibanDestino: sortear(outrasMoedas).iban,
    valor: valorAleatorio(50, origem.moeda),
    moedaDoValor: origem.moeda,
    emailNotificacao: emailDeVezEmQuando(),
  });
}

// Um em cada cinco pedidos com e-mail: as notificações também entram na carga (veja o Mailpit em :8025).
function emailDeVezEmQuando() {
  return Math.random() < 0.2 ? `carga-vu${__VU}@exemplo.com` : null;
}

function pedirSaga(rota, corpo) {
  const resposta = http.post(`${API_TRANSFERENCIAS}${rota}`, JSON.stringify(corpo), {
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': crypto.randomUUID(), ...CHAVE_TRANSFERENCIAS },
  });
  if (resposta.status === 202) {
    sagasIniciadas.add(1);
    if (resposta.json().estado === 'Agendada') {
      transferenciasAgendadas.add(1);
      console.error(`Transferência ${resposta.json().id} ficou Agendada: suba a transferencias-api com ` +
        'SEPA_HORARIO_DE_CORTE=false antes da carga.');
    }
  }
  return resposta;
}

export function teardown(dados) {
  const sagas = esperarSagasFinais(dados.contas);
  const convergiram = esperarConvergencia(dados.contas);
  if (convergiram) {
    conferirDinheiro(dados.contas, sagas);
  } else {
    console.error('Sem convergência entre Contas e Extrato, o balanço por moeda não é calculado.');
    moedasQueNaoFecham.add(1);
  }
}

// ----- Sagas ---------------------------------------------------------------------------------------------------

function esperarSagasFinais(contas) {
  const limite = Date.now() + ESPERA_SAGAS_S * 1000;
  let todas = listarSagas(contas);
  let pendentes = todas.filter((saga) => !terminou(saga));

  while (pendentes.length > 0 && Date.now() < limite) {
    sleep(3);
    todas = listarSagas(contas);
    pendentes = todas.filter((saga) => !terminou(saga));
  }

  for (const saga of pendentes) {
    console.error(`Saga ${saga.id} (${saga.tipo}, IBAN ${saga.ibanDestino}) ainda em ${saga.estado}`);
    sagasPendentes.add(1);
  }

  check(pendentes, { 'todas as sagas chegaram ao estado final': (p) => p.length === 0 }, { etapa: 'sagas' });
  console.log(`Sagas: ${todas.length - pendentes.length}/${todas.length} em estado final ` +
    `(${resumoPorEstado(todas)}).`);
  return detalhar(todas);
}

// Final 94 conclui e depois é devolvida; final 95 é estornada e depois fica Divergente. Até lá, não terminaram.
function terminou(saga) {
  if (!ESTADOS_FINAIS.includes(saga.estado)) {
    return false;
  }
  if (saga.tipo !== 'Externa') {
    return true;
  }
  const final = saga.ibanDestino.slice(-2);
  if (final === '94' && saga.estado === 'Concluida') {
    return false;
  }
  if (final === '95' && saga.estado === 'Cancelada' && saga.motivo === 'SemRespostaDoBanco') {
    return false;
  }
  return true;
}

function listarSagas(contas) {
  const sagas = [];
  for (const conta of contas) {
    let pagina = 1;
    while (true) {
      const resposta = http.get(
        `${API_TRANSFERENCIAS}/transferencias?contaOrigemId=${conta.id}&pagina=${pagina}&tamanho=200`,
        { headers: CHAVE_TRANSFERENCIAS });
      const lista = resposta.json();
      sagas.push(...lista.itens);
      if (pagina * lista.tamanho >= lista.total) {
        break;
      }
      pagina++;
    }
  }
  return sagas;
}

// A listagem não traz o valor debitado nem a conta de destino: busca o detalhe de cada saga, em lotes.
function detalhar(sagas) {
  const detalhes = [];
  for (let i = 0; i < sagas.length; i += 20) {
    const lote = sagas.slice(i, i + 20).map((saga) => ['GET', `${API_TRANSFERENCIAS}/transferencias/${saga.id}`, null,
      { headers: CHAVE_TRANSFERENCIAS }]);
    for (const resposta of http.batch(lote)) {
      detalhes.push(resposta.json());
    }
  }
  return detalhes;
}

function resumoPorEstado(sagas) {
  const contagem = {};
  for (const saga of sagas) {
    contagem[saga.estado] = (contagem[saga.estado] || 0) + 1;
  }
  return Object.entries(contagem).map(([estado, quantidade]) => `${estado}: ${quantidade}`).join(', ');
}

// ----- Contas x Extrato -----------------------------------------------------------------------------------------

function esperarConvergencia(contas) {
  const limite = Date.now() + ESPERA_CONVERGENCIA_S * 1000;
  let pendentes = contas;

  while (pendentes.length > 0 && Date.now() < limite) {
    pendentes = pendentes.filter((conta) => !convergiu(conta));
    if (pendentes.length > 0) {
      sleep(2);
    }
  }

  for (const conta of pendentes) {
    const daConta = http.get(`${API_CONTAS}/contas/${conta.id}`, { headers: CHAVE_CONTAS }).json();
    const extrato = http.get(`${API_EXTRATO}/extratos/${conta.id}?tamanho=1`, { headers: CHAVE_EXTRATO }).json();
    console.error(`Divergente ${conta.id}: Contas saldo=${daConta.saldo} v${daConta.versao}; ` +
      `Extrato saldo=${extrato.saldoAtual} v${extrato.ultimaVersao}`);
    divergencias.add(1);
  }

  check(pendentes, { 'todas as contas convergiram no Extrato': (p) => p.length === 0 }, { etapa: 'convergencia' });
  console.log(`Convergência: ${contas.length - pendentes.length}/${contas.length} contas iguais em Contas e Extrato.`);
  return pendentes.length === 0;
}

function convergiu(conta) {
  const daConta = http.get(`${API_CONTAS}/contas/${conta.id}`, { headers: CHAVE_CONTAS });
  const extrato = http.get(`${API_EXTRATO}/extratos/${conta.id}?tamanho=1`,
    { headers: CHAVE_EXTRATO, responseCallback: http.expectedStatuses(200, 404) });
  if (daConta.status !== 200 || extrato.status !== 200) {
    return false;
  }
  const c = daConta.json();
  const e = extrato.json();
  return c.versao === e.ultimaVersao && Number(c.saldo) === Number(e.saldoAtual);
}

// ----- Balanço por moeda ----------------------------------------------------------------------------------------

function conferirDinheiro(contas, sagas) {
  const balanco = {};
  for (const moeda of MOEDAS) {
    balanco[moeda] = { saldos: 0, depositos: 0, saques: 0, saidas: 0, entradas: 0 };
  }

  for (const conta of contas) {
    balanco[conta.moeda].saldos += centavos(http.get(`${API_CONTAS}/contas/${conta.id}`, { headers: CHAVE_CONTAS }).json().saldo);
    for (const lancamento of lancamentos(conta)) {
      if (lancamento.tipo === 'Deposito') {
        balanco[conta.moeda].depositos += centavos(lancamento.valor);
      } else if (lancamento.tipo === 'Saque') {
        balanco[conta.moeda].saques += centavos(lancamento.valor);
      }
    }
  }

  const moedaDaConta = Object.fromEntries(contas.map((conta) => [conta.id, conta.moeda]));
  for (const saga of sagas) {
    balanco[saga.moedaOrigem].saidas += saidaDaOrigem(saga);
    if (saga.tipo === 'CambioInterno' && saga.estado === 'Concluida') {
      balanco[moedaDaConta[saga.contaDestinoId] || saga.moedaDestino].entradas += centavos(saga.valorDestino);
    }
  }

  for (const moeda of MOEDAS) {
    const b = balanco[moeda];
    const esperado = b.depositos - b.saques - b.saidas + b.entradas;
    const fecha = esperado === b.saldos;
    console.log(`${moeda}: saldos ${emReais(b.saldos)} | depósitos ${emReais(b.depositos)} - saques ${emReais(b.saques)} ` +
      `- saídas das sagas ${emReais(b.saidas)} + câmbio recebido ${emReais(b.entradas)} = ${emReais(esperado)} ` +
      `${fecha ? 'fecha' : `NÃO FECHA (diferença ${emReais(b.saldos - esperado)})`}`);
    if (!fecha) {
      moedasQueNaoFecham.add(1);
    }
  }

  check(balanco, {
    'nenhum dinheiro sumiu (saldos e saídas fecham em cada moeda)': (b) =>
      MOEDAS.every((moeda) => b[moeda].saldos === b[moeda].depositos - b[moeda].saques - b[moeda].saidas + b[moeda].entradas),
  }, { etapa: 'dinheiro' });
}

// O que saiu de vez da conta de origem, na moeda dela. Cancelada e Recusada: nada (estornada ou nunca debitada).
// Concluida: valor e tarifa. Devolvida: só a tarifa. Divergente: depende do porquê.
function saidaDaOrigem(saga) {
  const valorETarifa = centavos(saga.valorDebitado) + centavos(saga.tarifa);
  switch (saga.estado) {
    case 'Concluida':
      return valorETarifa;
    case 'Devolvida':
      return centavos(saga.tarifa);
    case 'Divergente':
      // Confirmada depois do estorno: o dinheiro voltou e espera a reconciliação. Devolução não creditada: saiu.
      return saga.motivo === 'DevolucaoNaoCreditada' ? valorETarifa : 0;
    default:
      return 0;
  }
}

function lancamentos(conta) {
  const todos = [];
  let pagina = 1;
  while (true) {
    const extrato = http.get(`${API_EXTRATO}/extratos/${conta.id}?pagina=${pagina}&tamanho=200`,
      { headers: CHAVE_EXTRATO }).json();
    todos.push(...extrato.lancamentos);
    if (pagina * extrato.tamanho >= extrato.total) {
      break;
    }
    pagina++;
  }
  return todos;
}
