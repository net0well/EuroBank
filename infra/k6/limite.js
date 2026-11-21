// O http do k6 respeitando o rate limit das APIs: um 429 espera o Retry-After e repete, como um cliente
// bem-comportado, até TENTATIVAS_429 vezes. O total de 429 aparece na métrica respostas_429.
import http from 'k6/http';
import { sleep } from 'k6';
import { Counter } from 'k6/metrics';

const TENTATIVAS_429 = 20;
const respostas429 = new Counter('respostas_429');

/** Envia e, enquanto a API responder 429, espera o Retry-After e repete. */
function respeitandoLimite(enviar) {
  let resposta = enviar();
  for (let tentativa = 1; resposta.status === 429 && tentativa <= TENTATIVAS_429; tentativa++) {
    respostas429.add(1);
    sleep(Number(resposta.headers['Retry-After'] || 1));
    resposta = enviar();
  }
  return resposta;
}

/** O http do k6 com o 429 tratado; no lote, só as respostas 429 são repetidas, uma a uma. */
export const api = {
  get: (url, params) => respeitandoLimite(() => http.get(url, params)),
  post: (url, corpo, params) => respeitandoLimite(() => http.post(url, corpo, params)),
  batch: (pedidos) => http.batch(pedidos).map((resposta, i) =>
    resposta.status === 429 ? respeitandoLimite(() => http.request(...pedidos[i])) : resposta),
};
