# EuroBank

Laboratório de estudo de **CDC (Change Data Capture)** com **Debezium**, **Kafka** e **.NET 10**, organizado em
DDD com Clean Architecture. É um banco fictício com dois contextos que conversam apenas por eventos:

- **Contas**: abre contas, deposita, saca, transfere, bloqueia, desbloqueia e encerra. Grava no PostgreSQL.
- **Extrato**: consome os eventos de Contas pelo Kafka e monta o extrato de cada conta.

A API de Contas nunca fala com o Kafka. Ela grava a alteração da conta e o evento na tabela `outbox`, na mesma
transação, e o Debezium lê essas mudanças no log de transações (WAL) do PostgreSQL e as publica no Kafka.
Tudo sobe com um único `docker compose up`.

## Por que este laboratório existe

O objetivo é aprender, com código que roda e pode ser quebrado de propósito, os problemas que aparecem quando
dois sistemas precisam ficar consistentes sem compartilhar uma transação:

1. **Entender o CDC baseado em log.** Ver o Debezium se conectar ao PostgreSQL como uma réplica, ler o WAL por
   um replication slot e transformar cada alteração confirmada em mensagem, sem triggers e sem consultas
   periódicas às tabelas.
2. **Resolver o dual write com o Outbox Pattern.** Gravar no banco e publicar no broker são duas operações em
   sistemas diferentes; sem cuidado, uma acontece e a outra não. A outbox torna as duas atômicas.
3. **Comparar outbox e CDC bruto lado a lado.** O mesmo depósito aparece como evento de negócio
   (`DepositoRealizado`) e como a linha da tabela com o estado anterior e o posterior.
4. **Consumir com segurança.** A entrega do Kafka é "pelo menos uma vez": duplicatas, reprocessamento, ordem
   por partição, mensagens inválidas e escala horizontal precisam ser tratados no consumidor.
5. **Observar o fluxo inteiro.** Um único trace no Jaeger atravessa a API, a outbox, o Debezium, o Kafka e o
   consumidor.
6. **Praticar DDD, Clean Architecture e TDD** num domínio com regras reais (saldo, estados da conta,
   concorrência) e com testes que usam a infraestrutura de verdade.
7. **Preparar o terreno para sagas.** A próxima etapa usa a mesma outbox para orquestrar uma transferência
   para outro banco, com compensação.

## O que foi construído

| Parte | O que faz |
|---|---|
| Domínio de Contas | Agregado `Conta` com estados (Ativa, Bloqueada, Encerrada), versão de negócio, value objects `Dinheiro` (EUR), `Iban` (dígito verificador módulo 97) e `Titular`, serviço de domínio de transferência atômica entre contas |
| API de Contas | Minimal API com Wolverine como mediator, erros de domínio em ProblemDetails (400, 404, 409, 422), Swagger, seed de contas de exemplo |
| Outbox | A unidade de trabalho grava conta e eventos na mesma transação, na ordem em que os eventos aconteceram |
| Debezium | Dois conectores: Outbox Event Router (eventos de negócio) e CDC bruto da tabela `contas` (before e after) |
| Extrato | Worker com Wolverine consumindo o Kafka: inbox durável, ordem por conta, retry, dead letter e a projeção do extrato com paginação |
| Infraestrutura | Docker Compose com PostgreSQL em replicação lógica, Kafka em modo KRaft, Kafka Connect, Kafbat UI e Jaeger, com os conectores registrados automaticamente |
| Observabilidade | Serilog estruturado e OpenTelemetry, com o `traceparent` levado pela outbox até o consumidor |
| Testes | 127 testes: unitários, de integração com Testcontainers e ponta a ponta com Debezium real |
| Carga | Script k6 que gera operações em paralelo e confere se o Extrato convergiu para Contas |

## Como funciona

O caminho de um depósito:

1. A API recebe `POST /contas/{id}/depositos`. O agregado `Conta` valida as regras, atualiza o saldo,
   incrementa a versão e registra o evento de domínio `DepositoRealizado`.
2. A unidade de trabalho abre uma transação, faz o `UPDATE` em `contas` e o `INSERT` do evento na `outbox`, e
   confirma as duas coisas juntas.
3. O PostgreSQL escreve essas mudanças no WAL antes de confirmar o commit.
4. O Debezium lê o WAL pelos replication slots e publica:
   - pelo Outbox Event Router, o evento no tópico `eurobank.contas.eventos`, com o id da conta como chave e
     os headers `id`, `tipo` e `traceparent`;
   - pelo conector de CDC bruto, a linha alterada em `eurobank.cdc.public.contas`.
5. O worker do Extrato recebe a mensagem. O header `tipo` escolhe o manipulador, o header `id` é a chave de
   deduplicação da inbox e o `traceparent` continua o trace.
6. A projeção aplica o evento só se a versão for nova. Mensagens repetidas não alteram o saldo.

Dois padrões lado a lado:

| | Outbox | CDC bruto |
|---|---|---|
| Tabela capturada | `outbox` | `contas` |
| Tópico | `eurobank.contas.eventos` | `eurobank.cdc.public.contas` |
| Conteúdo | evento de negócio com contrato próprio | a linha da tabela: `before`, `after`, `op`, `source` |
| Chave | id da conta (ordem garantida por conta) | `{ "id": ... }` da linha |
| Headers | `id` (id do evento), `tipo`, `traceparent` | metadados do Debezium |
| Quem consome | o Extrato | ninguém: só inspeção na UI |

Em um sistema real, a integração entre serviços normalmente usa só a outbox. O CDC bruto serve para levar
dados a outros sistemas (data warehouse, índice de busca, cache). Aqui os dois existem para comparação.

## O que o laboratório revelou

Problemas reais encontrados durante a construção, cada um com um teste que o reproduz:

1. **Eventos gravados antes de o replication slot existir nunca são publicados.** O seed criava as contas de
   exemplo antes de os conectores serem registrados e, sem snapshot, esses eventos se perdiam. Solução:
   registrar os conectores antes de a aplicação gravar qualquer dado.
2. **O EF Core reordena os INSERTs de um mesmo SaveChanges.** O Debezium publica na ordem do WAL, então um
   depósito chegou ao Kafka antes da abertura da própria conta. Solução: a unidade de trabalho insere as linhas
   da outbox uma a uma, na ordem dos eventos, e a tabela ganhou a coluna `sequencia` com a ordem física de
   inserção.
3. **Duplicatas exigem mais de uma defesa.** A inbox durável do Wolverine descarta mensagens repetidas pelo id
   durante 7 dias. Depois disso, ou se a inbox for apagada, a regra de versão do extrato descarta o evento. Um
   índice único por conta e versão é a última barreira no banco. No reprocessamento completo do tópico com a
   inbox apagada, 8.818 mensagens foram descartadas e nenhum saldo mudou.
4. **Retry não conserta mensagem inválida.** Falhas transitórias (banco fora, timeout) têm novas tentativas;
   mensagens sem o header `tipo` ou com corpo inválido vão direto para o dead letter, e o consumo continua.
5. **Ordem e paralelismo convivem por chave.** Eventos da mesma conta são processados em ordem, contas
   diferentes em paralelo, e com 3 instâncias do Extrato cada uma assume uma partição.
6. **Observabilidade também precisa de filtro.** As consultas periódicas do Wolverine ao banco geravam traces
   avulsos que escondiam os traces das operações. Um amostrador descarta chamadas que não pertencem a nenhuma
   requisição ou mensagem.

## Arquitetura

```mermaid
flowchart LR
    cliente([Cliente / k6]) -->|HTTP| contas[API de Contas<br/>:5100]
    contas -->|"UPDATE contas + INSERT outbox<br/>(uma transação)"| pg[(PostgreSQL<br/>eurobank_contas)]
    pg -->|WAL| dbz[Kafka Connect<br/>Debezium]
    dbz -->|Outbox Event Router| t1[[eurobank.contas.eventos<br/>3 partições]]
    dbz -->|CDC bruto| t2[[eurobank.cdc.public.contas]]
    t1 -->|consumer group<br/>eurobank-extrato| extrato[Worker do Extrato<br/>:5200]
    extrato --> pg2[(PostgreSQL<br/>eurobank_extrato)]
    contas -. traces .-> jaeger[Jaeger]
    extrato -. traces .-> jaeger
```

## Tecnologias

| Peça | Versão |
|---|---|
| .NET | 10 |
| PostgreSQL | 18.6 |
| Apache Kafka (KRaft) | 4.4.0 |
| Debezium (Kafka Connect) | 3.6.3.Final |
| Wolverine | 6.60 |
| Entity Framework Core e Npgsql | 10 |
| Kafbat UI | 1.5.0 |
| Jaeger | 2.22 |
| k6 | 2.3 |
| xUnit v3, Shouldly, Testcontainers | 4.0, 4.3, 4.16 |

## Pré-requisitos

- Docker com Docker Compose (o compose completo usa cerca de 6 GB de RAM)
- .NET SDK 10.0.400 ou mais recente (para rodar testes ou depurar pela IDE)

## Como subir

```bash
docker compose up -d --build
```

A ordem de subida é controlada pelo compose: Postgres e Kafka, criação do tópico com 3 partições, Kafka Connect,
registro dos conectores, API de Contas (aplica as migrations e cria as contas de exemplo Ana, Bruno e Carla) e o
worker do Extrato.

| Serviço | URL |
|---|---|
| API de Contas (Swagger) | http://localhost:5100/swagger |
| Extrato (Swagger) | http://localhost:5200/swagger |
| Kafbat UI (tópicos, mensagens, consumer groups, conectores) | http://localhost:8080 |
| Kafka Connect (REST) | http://localhost:8083/connectors |
| Jaeger (traces) | http://localhost:16686 |
| PostgreSQL | `localhost:5434`, usuário e senha `eurobank` (porta configurável em `.env`) |
| Kafka (para apps no host) | `localhost:9092` |

As credenciais existem só para o ambiente local do laboratório.

Roteiros de requisições prontos: [`http/contas.http`](http/contas.http) e [`http/extrato.http`](http/extrato.http).

Para recomeçar do zero (apaga os dados do laboratório): `docker compose down -v`.

## Estrutura

```
src/
  Shared/   EuroBank.Shared (blocos de domínio, Dinheiro, Paginacao)
            EuroBank.Shared.Web (Serilog, OpenTelemetry, erros em ProblemDetails)
  Contas/   Domain | Application | Infrastructure | Api
  Extrato/  Domain | Application | Infrastructure | Worker
tests/      testes unitários, de integração (Testcontainers) e ponta a ponta (Debezium de verdade)
infra/      init do Postgres, criação do tópico, conectores do Debezium, script do k6
http/       roteiros de requisições
```

Convenções: nomes de projetos e pastas técnicas em inglês (`Entities`, `ValueObjects`, `Events`, `Exceptions`,
`TransferObjects`...), classes de negócio e métodos em português, uma classe por arquivo. Os dois contextos não
referenciam um ao outro: o Extrato tem os próprios DTOs para os eventos que consome.

Pontos do código que valem a leitura:

- `src/Contas/EuroBank.Contas.Domain/Entities/Conta.cs`: o agregado decide qual evento acontece.
- `src/Contas/EuroBank.Contas.Infrastructure/Persistence/UnidadeDeTrabalho.cs`: estado e outbox na mesma
  transação, com as linhas inseridas na ordem dos eventos.
- `infra/connect/conectores/eurobank-contas-outbox.json`: como cada coluna da outbox vira tópico, chave, header
  ou corpo da mensagem.
- `src/Extrato/EuroBank.Extrato.Infrastructure/Messaging/MapeadorEnvelopeDebezium.cs`: como o Wolverine lê os
  headers do Debezium.
- `src/Extrato/EuroBank.Extrato.Infrastructure/Messaging/ConfiguracaoMensageria.cs`: inbox durável, ordem por
  conta, retry e dead letter.
- `src/Extrato/EuroBank.Extrato.Domain/Entities/ExtratoConta.cs`: a regra de versão que torna a projeção
  idempotente.

## Depurar pela IDE

A infra continua no Docker; só as aplicações rodam no host.

```bash
docker compose stop contas-api extrato-worker
dotnet run --project src/Contas/EuroBank.Contas.Api
dotnet run --project src/Extrato/EuroBank.Extrato.Worker
```

Os `appsettings.json` já apontam para `localhost:5434` (Postgres) e `localhost:9092` (Kafka), e as portas
continuam 5100 e 5200.

## Testes

```bash
dotnet test
```

Precisa do Docker rodando: os testes de integração e ponta a ponta sobem os próprios containers com
Testcontainers. Os testes ponta a ponta usam os mesmos conectores de `infra/connect/conectores` e cobrem o
evento nos dois tópicos, a convergência do extrato, o trace contínuo, o Kafka Connect parado e religado sem
perda, a duplicata barrada pela inbox e as mensagens inválidas no dead letter.

## Roteiro de exercícios

### 1. Seguir uma operação até o extrato

Faça um depósito pelo `http/contas.http`. No Kafbat UI, abra os dois tópicos e compare: em
`eurobank.contas.eventos`, o evento `DepositoRealizado` com os headers `id`, `tipo` e `traceparent`; em
`eurobank.cdc.public.contas`, a linha da conta com o saldo antes e depois. Depois consulte o extrato pelo
`http/extrato.http`.

### 2. Kafka Connect fora do ar

```bash
docker compose stop connect
# faça algumas operações pelo contas.http
docker compose exec postgres psql -U eurobank -d eurobank_contas -c "SELECT slot_name, active, confirmed_flush_lsn, pg_current_wal_lsn() FROM pg_replication_slots;"
docker compose start connect
```

Com o Connect parado, o `confirmed_flush_lsn` dos slots não avança e o WAL fica retido. Ao religar, o Debezium
retoma do ponto salvo e todos os eventos chegam ao Extrato.

### 3. Reprocessar o tópico (duas camadas de idempotência)

```bash
docker compose stop extrato-worker
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server kafka:29092 \
  --group eurobank-extrato --topic eurobank.contas.eventos --reset-offsets --to-earliest --execute
docker compose start extrato-worker
```

Os saldos não mudam: a inbox durável do Wolverine ainda lembra os ids das mensagens (retenção de 7 dias) e as
descarta. Para ver a segunda camada, repita apagando também a memória da inbox antes de religar o worker:

```bash
docker compose exec postgres psql -U eurobank -d eurobank_extrato -c "DELETE FROM wolverine.wolverine_incoming_envelopes;"
```

Agora as mensagens chegam aos manipuladores, e o log do worker mostra
`ignorado: a versão N já foi aplicada`. A regra de versão do `ExtratoConta` impede que o saldo mude.

### 4. Dead letter

No Kafbat UI, produza uma mensagem em `eurobank.contas.eventos` sem o header `tipo`, ou com `tipo` igual a
`DepositoRealizado` e um corpo que não seja JSON. Ela vai direto para o dead letter (tentar de novo não
resolveria), e o consumo das mensagens seguintes continua:

```bash
docker compose exec postgres psql -U eurobank -d eurobank_extrato -c "SELECT id, message_type, exception_type, exception_message FROM wolverine.wolverine_dead_letters;"
```

Mensagens com um `tipo` desconhecido não são erro: são ignoradas com um alerta no log (tolerant reader).

### 5. Escalar o Extrato para 3 instâncias

```bash
docker compose -f docker-compose.yml -f docker-compose.escala.yml up -d --scale extrato-worker=3
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server kafka:29092 \
  --describe --group eurobank-extrato
```

Cada instância fica com uma das 3 partições. Como a chave da mensagem é o id da conta, os eventos de uma conta
sempre caem na mesma partição e continuam em ordem. Para voltar a uma instância: `docker compose up -d`.

### 6. Carga com k6

```bash
docker compose --profile carga run --rm k6
docker compose --profile carga run --rm -e VUS=20 -e DURACAO=2m k6
```

O script abre contas, dispara depósitos, saques e transferências em paralelo e, no fim, confere conta a conta se
o Extrato convergiu para o saldo e a versão de Contas. Numa máquina de desenvolvimento, 10 usuários virtuais
fazem cerca de 160 operações por segundo sem erros.

### 7. Trace ponta a ponta

No Jaeger, escolha o serviço `eurobank-contas` e abra o trace de um `POST`. O mesmo trace contém o processamento
no `eurobank-extrato`: o `traceparent` gravado na outbox atravessa o Debezium e o Kafka como header.

### 8. Outbox que se apaga sozinha

```bash
OUTBOX_APAGAR_APOS_GRAVAR=true docker compose up -d contas-api
```

(No PowerShell: `$env:OUTBOX_APAGAR_APOS_GRAVAR="true"; docker compose up -d contas-api`.)

Agora cada linha da outbox é inserida e apagada na mesma transação. A tabela fica vazia, mas os eventos
continuam chegando ao Kafka: o Debezium lê o INSERT no WAL, não a tabela.

## Laboratório e produção

Atalhos que este laboratório toma de propósito:

| No laboratório | Em produção |
|---|---|
| Publications `FOR ALL TABLES`, para registrar os conectores antes de as tabelas existirem | Publication criada por migration ou pelo DBA, só com as tabelas capturadas |
| Debezium conecta com o usuário dono do banco | Usuário dedicado com permissão `REPLICATION` e leitura só nas tabelas capturadas |
| Kafka com um nó, sem volume, fator de replicação 1 | Cluster com fator de replicação 3 e dados persistentes |
| JSON sem schema | Muitas vezes Avro ou Protobuf com Schema Registry, para versionar contratos |
| Sem monitoramento dos replication slots | Alerta de atraso de slot (um slot parado enche o disco com WAL) e heartbeat configurado |
| CDC bruto e outbox no mesmo banco, para comparar | Integração entre serviços pela outbox; CDC bruto só para levar dados a outros sistemas |
| APIs sem autenticação | Autenticação e autorização em todas as APIs |

## Próximos passos

Em andamento: uma **saga orquestrada** de transferência para outro banco. Um novo contexto, `Transferencias`,
coordena o débito na conta de origem, a liquidação num banco externo simulado e, se a liquidação falhar, o
estorno do débito. Comandos e respostas usam a mesma combinação de outbox, Debezium e Kafka, com timeouts,
reenvio, cancelamento pelo cliente, tarifa, agendamento e limite diário.
