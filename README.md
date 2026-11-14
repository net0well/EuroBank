# EuroBank

Laboratório de estudo de **CDC (Change Data Capture)** com **Debezium**, **Kafka** e **.NET 10**, organizado em
DDD com Clean Architecture, e de **saga orquestrada** sobre essa mesma base. É um banco fictício com quatro
contextos que conversam apenas por mensagens, mais dois sistemas "de fora" simulados:

- **Contas**: abre contas em EUR, USD, GBP ou BRL, deposita, saca, transfere, bloqueia, desbloqueia e encerra.
  Grava no PostgreSQL.
- **Extrato**: consome os eventos de Contas pelo Kafka e monta o extrato de cada conta, na moeda dela.
- **Transferencias**: orquestra a transferência para um IBAN de outro banco (débito em Contas, liquidação no
  banco externo e, se a liquidação falhar, estorno do débito) e o câmbio entre contas EuroBank de moedas
  diferentes (cotação, débito, crédito), com prazos, reenvios, cancelamento, regras SEPA com horário de corte,
  devolução pelo banco e reconciliação quando o dinheiro não fecha.
- **Banco externo simulado**: um sistema "de fora" que liquida, recusa, atrasa, some, confirma tarde demais ou
  devolve, conforme o final do IBAN.
- **Câmbio simulado**: um provedor de cotações "de fora", com spread e cotação travada por 10 minutos.
- **Notificacoes**: quando uma transferência termina, manda um e-mail em pt-BR para quem pediu (no Mailpit, uma
  caixa de e-mail de laboratório).

Nenhuma API do EuroBank fala com o Kafka. Cada uma grava a alteração e a mensagem na tabela `outbox` do seu
banco, na mesma transação, e o Debezium lê essas mudanças no log de transações (WAL) do PostgreSQL e as publica
no Kafka. Tudo sobe com um único `docker compose up`.

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
7. **Orquestrar uma saga com compensação.** A transferência para outro banco envolve três sistemas, cada um com
   o próprio banco de dados. O orquestrador é um agregado DDD com o estado no PostgreSQL; comandos e respostas
   passam pela mesma combinação de outbox, Debezium e Kafka. Respostas que não chegam, chegam atrasadas ou
   chegam duas vezes são o caso normal, não a exceção.

## O que foi construído

| Parte | O que faz |
|---|---|
| Domínio de Contas | Agregado `Conta` com estados (Ativa, Bloqueada, Encerrada), moeda escolhida na abertura, versão de negócio, value objects `Dinheiro` (valor e moeda; operar moedas diferentes é proibido), `Saldo` (pode ficar negativo, mas só por reconciliação), `Iban` (dígito verificador módulo 97) e `Titular`, serviço de domínio de transferência atômica entre contas da mesma moeda |
| API de Contas | Minimal API com Wolverine como mediator, erros de domínio em ProblemDetails (400, 404, 409, 422), Swagger, seed de contas de exemplo |
| Outbox | A unidade de trabalho grava conta e eventos na mesma transação, na ordem em que os eventos aconteceram |
| Debezium | Dois conectores: Outbox Event Router (eventos de negócio) e CDC bruto da tabela `contas` (before e after) |
| Extrato | Worker com Wolverine consumindo o Kafka: inbox durável, ordem por conta, retry, dead letter e a projeção do extrato com paginação, incluindo transferências externas com tarifa, estornos, devoluções e reconciliações (com saldo negativo) |
| Sagas de transferência | Agregado `Transferencia` com a máquina de estados da externa e do câmbio interno, cotação antes do débito quando as moedas diferem (com recotação), prazo por passo, até 3 reenvios, consulta de status antes de estornar, cancelamento pelo cliente, agendamento, tarifa e limite diário por moeda, validação SEPA e SWIFT com horário de corte, estado `Divergente` com reconciliação e devolução pelo banco externo; histórico de cada passo |
| API de Transferências | `POST /transferencias-externas` e `POST /transferencias-cambio` com `Idempotency-Key`, consulta com histórico e cotação, listagem com filtros, cancelamento e `POST /transferencias-externas/{id}/reconciliacao`; o limite diário é conferido com um advisory lock por conta |
| Worker de Transferências | Respostas de Contas, do câmbio e do banco externo (inbox durável), a projeção local das contas (eventos `ContaAberta` e `ContaEncerrada`) e uma varredura que começa as agendadas e trata os prazos com `FOR UPDATE SKIP LOCKED` |
| Worker de Contas | Executa os comandos da saga (debitar, creditar, estornar, liberar a trava, debitar a reconciliação, creditar a devolução) com idempotência pelo id da transferência e responde pela outbox; a trava semântica impede encerrar a conta no meio da saga |
| Simuladores | Banco externo e câmbio com Confluent.Kafka e Npgsql puros, sem Wolverine e sem EF: inbox escrita à mão e respostas agendadas no banco, compartilhadas pelos dois |
| Notificações | Worker com Wolverine consumindo `eurobank.transferencias.eventos`: e-mail em pt-BR (HTML e texto, pelo MailKit) para o Mailpit, inbox durável mais a tabela `notificacoes` com o id do evento como chave (reprocessar o tópico não repete e-mail), novas tentativas agendadas quando o SMTP falha e dead letter depois delas |
| Segurança | Uma API key por API (`X-Api-Key`), com `/health` e Swagger abertos |
| Infraestrutura | Docker Compose com PostgreSQL em replicação lógica, Kafka em modo KRaft, Kafka Connect, Kafbat UI, Jaeger e Mailpit, com os databases, os tópicos e os conectores criados automaticamente |
| Observabilidade | Serilog estruturado e OpenTelemetry, com o `traceparent` levado pela outbox até o consumidor e de volta nas respostas da saga |
| Testes | 496 testes: unitários, de integração com Testcontainers e ponta a ponta com Debezium real, incluindo cada caminho das sagas e os e-mails no Mailpit |
| Carga | Script k6 que mistura operações de Contas com sagas externas e de câmbio e, no fim, confere que todas as sagas terminaram, que o Extrato convergiu e que nenhum dinheiro sumiu, moeda a moeda |

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

### O caminho de uma transferência externa

1. A API de Transferências recebe `POST /transferencias-externas` com o header `Idempotency-Key`. O agregado
   `Transferencia` valida o pedido (IBAN de outro banco, BIC, valor e, em euro, as regras SEPA), calcula a
   tarifa e, se for para já, confere o limite diário e pede o débito. Estado e comando
   `DebitarTransferenciaExterna` vão juntos para o banco: a saga e a outbox, na mesma transação.
2. O Debezium publica o comando em `eurobank.contas.comandos`, com o id da conta como chave. O worker de Contas
   debita valor e tarifa, guarda o id da transferência (idempotência e trava contra encerramento) e responde
   `DebitoRealizado` pela outbox de Contas, em `eurobank.contas.respostas`.
3. O worker de Transferências aplica a resposta e pede a liquidação ao banco externo
   (`eurobank.banco-externo.comandos`). O banco simulado responde em `eurobank.banco-externo.respostas`.
4. Com `LiquidacaoConfirmada`, a saga termina `Concluida` e manda `LiberarTravaTransferencia` a Contas. Com
   `LiquidacaoRecusada`, pede o estorno e termina `Cancelada` quando Contas confirmar a devolução.
5. Em paralelo, a varredura do worker acompanha os prazos. Sem resposta no prazo, o comando é reenviado (com id
   novo; os participantes deduplicam pelo id da transferência). Esgotados os reenvios da liquidação, a saga
   consulta o status no banco antes de decidir pelo estorno.
6. O banco ainda pode falar depois do fim da saga: confirmar uma transferência já estornada (a saga fica
   `Divergente`) ou devolver uma já concluída (a saga pede o crédito a Contas e termina `Devolvida`). Veja
   [Divergência, reconciliação e devolução](#divergência-reconciliação-e-devolução).
7. O evento final da saga vai para `eurobank.transferencias.eventos`, e o worker de notificações manda o e-mail.

A conversa entre os participantes, no caminho feliz e na recusa pelo banco (cada mensagem sai de uma outbox, passa
pelo Debezium e pelo Kafka; só o banco externo publica direto, por ser um sistema de fora):

```mermaid
sequenceDiagram
    autonumber
    actor Cliente
    participant API as API de Transferências
    participant TW as Worker de Transferências
    participant CW as Worker de Contas
    participant Banco as Banco externo
    participant NW as Worker de Notificações
    Cliente->>API: POST /transferencias-externas (Idempotency-Key)
    API-->>Cliente: 202 AguardandoDebito
    API->>CW: DebitarTransferenciaExterna (eurobank.contas.comandos)
    CW->>TW: DebitoRealizado (eurobank.contas.respostas)
    TW->>Banco: LiquidarTransferencia (eurobank.banco-externo.comandos)
    alt o banco confirma
        Banco->>TW: LiquidacaoConfirmada (eurobank.banco-externo.respostas)
        TW->>CW: LiberarTravaTransferencia
        TW->>NW: TransferenciaConcluida (eurobank.transferencias.eventos)
    else o banco recusa
        Banco->>TW: LiquidacaoRecusada
        TW->>CW: EstornarTransferencia
        CW->>TW: EstornoRealizado
        TW->>NW: TransferenciaCancelada
    end
    NW-->>Cliente: e-mail em pt-BR (Mailpit)
```

Os estados da saga, com todos os caminhos:

```mermaid
stateDiagram-v2
    [*] --> Agendada
    Agendada --> AguardandoDebito: na hora, mesma moeda (limite diário ok)
    Agendada --> Cotando: na hora, moedas diferentes
    Agendada --> Recusada: limite diário excedido
    Agendada --> Cancelada: cancelamento
    Cotando --> AguardandoDebito: CotacaoEmitida (limite diário ok)
    Cotando --> Cotando: cotação vencida
    Cotando --> Recusada: CotacaoIndisponivel, sem resposta ou limite
    Cotando --> Cancelada: cancelamento
    AguardandoDebito --> Debitada: DebitoRealizado
    AguardandoDebito --> Recusada: DebitoRecusado ou sem resposta
    Debitada --> AguardandoLiquidacao: externa
    Debitada --> AguardandoCredito: câmbio interno
    Debitada --> AguardandoEstorno: cancelamento anotado
    AguardandoCredito --> Concluida: CreditoRealizado
    AguardandoCredito --> AguardandoEstorno: CreditoRecusado (destino encerrado)
    AguardandoLiquidacao --> Concluida: LiquidacaoConfirmada
    AguardandoLiquidacao --> AguardandoEstorno: LiquidacaoRecusada
    AguardandoLiquidacao --> ConsultandoLiquidacao: reenvios esgotados
    ConsultandoLiquidacao --> Concluida: Liquidada
    ConsultandoLiquidacao --> AguardandoEstorno: NaoLiquidada ou sem resposta
    AguardandoEstorno --> Cancelada: EstornoRealizado
    AguardandoEstorno --> Divergente: EstornoRealizado, com o banco já tendo confirmado
    Recusada --> AguardandoEstorno: DebitoRealizado tardio
    Cancelada --> Divergente: LiquidacaoConfirmada tardia
    Divergente --> Reconciliando: POST reconciliacao
    Reconciliando --> Concluida: ReconciliacaoRealizada (Reconciliada)
    Reconciliando --> Divergente: ReconciliacaoRecusada
    Concluida --> AguardandoCreditoDevolucao: TransferenciaDevolvida
    AguardandoCreditoDevolucao --> Devolvida: DevolucaoCreditada
    AguardandoCreditoDevolucao --> Divergente: DevolucaoRecusada
    Concluida --> [*]
    Cancelada --> [*]
    Recusada --> [*]
    Devolvida --> [*]
    Divergente --> [*]
```

O banco externo simulado decide pelo final do IBAN de destino, para cada caminho poder ser reproduzido:

| Final | Comportamento | Fim da saga |
|---|---|---|
| `99` | recusa | `Cancelada` (`RecusadaPeloBanco`), com estorno |
| `98` | nunca responde, nem à consulta | `Cancelada` (`SemRespostaDoBanco`), depois dos reenvios e das consultas |
| `97` | responde em 45 s, depois do primeiro prazo | `Concluida`, com um reenvio e a resposta duplicada ignorada |
| `96` | liquida, mas a resposta se perde | `Concluida` pela consulta de status, sem estorno |
| `95` | a consulta diz NaoLiquidada, mas confirma depois de 5 min | `Cancelada` com estorno e, com a confirmação, `Divergente`; reconciliada, `Concluida` (`Reconciliada`) |
| `94` | confirma e, 10 s depois, devolve | `Concluida` e depois `Devolvida` (`DevolvidaPeloBanco`), com o valor de volta e sem a tarifa |
| outros | confirma em 1 s | `Concluida` |

Prazos padrão: cotação 30 s, débito 30 s, liquidação 30 s, consulta 15 s, crédito 30 s, estorno 30 s,
reconciliação 30 s e crédito da devolução 30 s, com até 3 reenvios por passo. Crédito, estorno, reconciliação e
crédito da devolução reenviam sem limite: desistir deles deixaria o dinheiro em dois lugares ou em nenhum.

### SEPA, SWIFT e horário de corte

Toda externa exige um BIC válido (8 ou 11 caracteres, no formato SWIFT). Quando a moeda de destino é o euro, a
transferência é SEPA:

- o IBAN de destino precisa ser de um país da área SEPA (os 27 da União Europeia, mais Islândia, Liechtenstein,
  Noruega, Suíça, Reino Unido, Mônaco, San Marino, Andorra e Vaticano); fora dela, 422 (`pais_fora_da_sepa`);
- pedidos depois das 16:00 UTC, ou no sábado e no domingo, ficam agendados para o próximo dia útil às 08:00 UTC
  (`executarEmEfetivo` na resposta). Outras moedas não têm horário de corte.

O horário de corte pode ser desligado com `Sepa:AplicarHorarioDeCorte=false` (no compose, `SEPA_HORARIO_DE_CORTE`),
para os testes e os exercícios não dependerem da hora em que rodam.

### Divergência, reconciliação e devolução

O fim da saga não é o fim da conversa com o banco externo:

- **Confirmação depois do estorno** (final `95`). A saga desistiu do banco, a consulta disse `NaoLiquidada`, o
  débito foi estornado e só então chega a `LiquidacaoConfirmada`: o valor está com o beneficiário e de volta na
  conta. A transferência fica `Divergente` (`ConfirmadaAposEstorno`), listada em
  `GET /transferencias?status=Divergente`. Se a confirmação chega com o estorno ainda em andamento, a saga só
  anota (o comando já está com Contas e não volta) e fica `Divergente` quando o estorno termina.
- **Reconciliação.** `POST /transferencias-externas/{id}/reconciliacao` pede a Contas que debite de novo valor e
  tarifa, mesmo sem saldo: é o único caminho que deixa uma conta negativa. A saga termina `Concluida`
  (`Reconciliada`). Com a conta de origem encerrada, 422 (`conta_origem_encerrada`); fora de `Divergente`, 409.
- **Saldo negativo.** Enquanto a conta estiver negativa, saques, transferências e novas externas são recusados
  (`SaldoNegativo`); depósitos e créditos são aceitos, e encerrar exige saldo zero.
- **Devolução** (final `94`). O banco devolve uma transferência concluída (`TransferenciaDevolvida`). A saga pede a
  Contas o crédito do valor, sem a tarifa (o serviço foi prestado), e termina `Devolvida`. Se Contas não puder
  creditar (conta encerrada), a transferência fica `Divergente` (`DevolucaoNaoCreditada`) para tratamento manual.

### Transferências com câmbio

As contas têm moeda (EUR, USD, GBP ou BRL), escolhida na abertura. Quando a moeda de destino é outra, a saga
começa cotando: manda `CotarCambio` ao câmbio simulado (`eurobank.cambio.comandos`) e só pede o débito quando
a cotação chega, porque só então se sabe quanto sai da conta. O limite diário é conferido nesse momento, com o
valor convertido.

- **Externa com câmbio**: `POST /transferencias-externas` com `moedaDestino` diferente da conta. O débito é na
  moeda da conta; a liquidação no banco externo, na moeda de destino.
- **Câmbio interno**: `POST /transferencias-cambio` para outra conta EuroBank de outra moeda. A saga debita a
  origem e credita o destino (estado `AguardandoCredito`); se o destino estiver encerrado, estorna a origem.
- O campo `valor` pode estar na moeda de origem ou na de destino (`moedaDoValor`): "mandar 100 EUR" ou "o destino
  recebe exatamente 100 USD".

O câmbio simulado usa base EUR (1 EUR = 1,08 USD = 0,86 GBP = 5,90 BRL), cruza os outros pares pelo euro,
aplica 1% de spread contra o cliente, trava a cotação por 10 minutos, arredonda meio para cima e não negocia o
par BRL e GBP. Exemplo: para o destino receber 100 USD, uma conta em euro paga 93,53 EUR à taxa de 1,0692, mais a
tarifa.

| Moeda da conta | Tarifa | Limite diário |
|---|---|---|
| EUR | 2,50 | 5.000 |
| USD | 2,70 | 5.400 |
| GBP | 2,20 | 4.300 |
| BRL | 15,00 | 30.000 |

Transferencias não consulta Contas: mantém uma **projeção local das contas** (id, IBAN, titular, moeda e se foi
encerrada) a partir dos eventos `ContaAberta` e `ContaEncerrada` de `eurobank.contas.eventos`. Uma conta
recém-aberta pode levar um instante para chegar à projeção; até lá, o pedido recebe 422
(`conta_origem_desconhecida`) e pode ser repetido com a mesma `Idempotency-Key`.

### Notificações por e-mail

Os eventos finais das sagas (`TransferenciaConcluida`, `TransferenciaCancelada`, `TransferenciaRecusada`,
`TransferenciaDivergente` e `TransferenciaDevolvida`) saem pela outbox de Transferencias em
`eurobank.transferencias.eventos` com um `resumo` da transferência: titular, beneficiário, IBAN, valores, tarifa,
cotação, descrição e o `emailNotificacao` do pedido. O worker de notificações não consulta ninguém: tudo o que o
e-mail precisa vem no evento (event-carried state transfer).

- Sem `emailNotificacao`, nenhum e-mail. Com um endereço inválido, um alerta no log e nada enviado.
- Um e-mail por evento, em pt-BR, com versão HTML e versão texto. Uma transferência pode gerar mais de um:
  `Divergente` e depois `Concluida` (`Reconciliada`), ou `Concluida` e depois `Devolvida`.
- Duplicatas: a inbox durável descarta a mensagem repetida por 7 dias; depois disso, a tabela `notificacoes`
  (chave: id do evento) encontra a notificação `Enviada` e não manda de novo.
- SMTP fora do ar: a notificação fica `Pendente`, com o número de tentativas e o último erro, e a mensagem volta
  em 5 s, 30 s e 2 min (nova tentativa agendada, que não segura as outras mensagens da partição). Esgotadas as
  tentativas, vai para o dead letter. A saga não é afetada: ela já terminou quando o evento foi publicado.
- O SMTP não participa da transação: se o processo cair entre o servidor aceitar o e-mail e a notificação virar
  `Enviada`, sai uma segunda cópia. O `Message-ID` é o id do evento, então as duas cópias são identificáveis.

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
7. **O orquestrador não sabe se o débito aconteceu.** Se Contas não responde, o comando continua no Kafka e
   pode ser executado depois que a saga desistiu. Solução: a saga termina `Recusada` (`SemRespostaDeContas`),
   guarda que o débito ficou sem resposta e, se o `DebitoRealizado` chegar mais tarde, pede o estorno.
8. **A varredura e as respostas disputam a mesma saga.** Uma resposta e um prazo vencido chegaram juntos na
   mesma transferência. A versão otimista e a chave do histórico (transferência e ordem do passo) fazem um dos
   dois falhar; o Wolverine tenta de novo com a saga recarregada e a resposta é aplicada no estado novo.
9. **Limite diário precisa de serialização por conta.** Seis pedidos simultâneos de 1.000 EUR na mesma conta
   passariam todos pela soma do dia. Um advisory lock do PostgreSQL por conta, até o commit, faz exatamente
   cinco passarem.
10. **Scripts de init do PostgreSQL só rodam num volume novo.** Num laboratório que já existia, o database
    novo não seria criado e o conector do Debezium falharia. Solução: um serviço que cria os databases que
    faltam a cada subida, antes do registro dos conectores.
11. **O reenvio por prazo também cobre a perda de mensagens.** Durante um teste, o Docker reiniciou e o Kafka
    (sem volume no laboratório) perdeu os comandos que ainda estavam nos tópicos. As sagas estavam salvas no
    PostgreSQL: quando os prazos venceram, a varredura reenviou os comandos e todas terminaram `Concluida`.
12. **No câmbio interno, o mesmo id de transferência aparece em duas contas.** A tabela que guarda as
    transferências de cada conta (trava e idempotência) tinha o id da transferência como chave; o crédito no
    destino colidiu com o débito na origem. A chave passou a ser conta e transferência.
13. **Uma projeção só conhece o que viu.** Transferencias aprende as contas pelos eventos `ContaAberta`; contas
    abertas antes de o worker existir, e cujos eventos já saíram da retenção do Kafka, não aparecem. Num
    laboratório novo isso não acontece; em produção, a resposta é retenção longa ou compactação do tópico, ou um
    snapshot inicial da projeção.
14. **Um conversor do EF não enxerga outra coluna.** Com o `Dinheiro` levando a moeda, o saldo não podia mais
    ser convertido sozinho de `numeric` para `Dinheiro`. Os agregados guardam o número num campo privado e
    montam o `Dinheiro` com a moeda da conta.
15. **Regra que depende do relógio quebra teste na hora errada.** Com o horário de corte SEPA, a mesma
    transferência fica para já ou para o próximo dia útil conforme a hora em que o teste roda. Os testes de
    domínio e de API controlam o tempo com `FakeTimeProvider`; os demais desligam o corte por configuração.
16. **Compensação pedida não se desfaz.** Se a confirmação do banco chega com o estorno já a caminho de Contas,
    não há como retirar o comando do Kafka. A saga anota a confirmação, deixa o estorno terminar e só então
    marca a transferência como `Divergente`, para a reconciliação corrigir.
17. **Um e-mail não se desfaz.** O SMTP não participa da transação do banco: a notificação é gravada `Pendente`
    antes do envio e vira `Enviada` depois. Se o processo cair entre os dois, a próxima tentativa manda outra
    cópia. A entrega continua "pelo menos uma vez"; o `Message-ID` com o id do evento deixa a cópia
    identificável.
18. **Retry imediato não serve para um servidor fora do ar.** Com a política comum do laboratório (esperas de 1 s
    e 3 s), um Mailpit parado por alguns segundos já mandaria a mensagem para o dead letter. As notificações
    agendam a nova tentativa (5 s, 30 s e 2 min): a mensagem sai da fila e volta depois, sem segurar as outras.
19. **Pelo `InvokeAsync`, a política de erros age diferente.** No teste que chamava o manipulador direto, o
    Wolverine fez a nova tentativa na hora, em vez de agendá-la, e a falha nunca chegou ao teste. O retry
    agendado só aparece com a mensagem numa fila (`PublishAsync` nos testes, o Kafka no laboratório).
20. **Um consumidor novo lê eventos antigos.** Os eventos finais só ganharam o `resumo` nesta fase; um worker que
    lê o tópico desde o início encontra eventos sem ele. O DTO aceita o resumo nulo e o evento é ignorado
    (tolerant reader), em vez de ir para o dead letter.
21. **Estado final não quer dizer que o dinheiro parou.** Uma `Concluida` do final `94` ainda vai ser devolvida,
    e uma `Cancelada` do final `95` ainda vai ficar `Divergente`. O balanço da carga só fecha quando espera esses
    passos tardios: para o k6, a 94 só termina `Devolvida` e a 95 só termina `Divergente`.

## Arquitetura

CDC de Contas para o Extrato:

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

Saga de transferência externa (cada seta para o Kafka sai de uma outbox, lida pelo Debezium, exceto a do banco
externo, que publica direto por ser um sistema de fora):

```mermaid
flowchart LR
    cliente([Cliente]) -->|HTTP + Idempotency-Key| tapi[API de Transferências<br/>:5300]
    tapi --> pgt[(eurobank_transferencias<br/>saga + outbox)]
    tworker[Worker de Transferências<br/>respostas + varredura + projeção] --> pgt
    pgt -->|Debezium| xc[[eurobank.cambio.comandos]]
    pgt -->|Debezium| cc[[eurobank.contas.comandos]]
    pgt -->|Debezium| bc[[eurobank.banco-externo.comandos]]
    pgt -->|Debezium| te[[eurobank.transferencias.eventos]]
    xc --> cambio[Câmbio simulado]
    cambio --> xr[[eurobank.cambio.respostas]]
    cc --> cworker[Worker de Contas]
    cworker --> pgc[(eurobank_contas<br/>conta + outbox)]
    pgc -->|Debezium| cr[[eurobank.contas.respostas]]
    pgc -->|Debezium| ce[[eurobank.contas.eventos]]
    bc --> banco[Banco externo simulado]
    banco --> br[[eurobank.banco-externo.respostas]]
    xr --> tworker
    cr --> tworker
    br --> tworker
    ce -->|ContaAberta e ContaEncerrada| tworker
    te --> nworker[Worker de Notificações]
    nworker --> pgn[(eurobank_notificacoes<br/>notificacoes + inbox)]
    nworker -->|SMTP| mailpit[Mailpit<br/>:8025]
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
| Confluent.Kafka (banco externo simulado) | 2.16 |
| MailKit (notificações) | 4.18 |
| Mailpit | 1.33 |
| Kafbat UI | 1.5.0 |
| Jaeger | 2.22 |
| k6 | 2.3 |
| xUnit v3, Shouldly, Testcontainers | 4.0, 4.3, 4.16 |

## Pré-requisitos

- Docker com Docker Compose (o compose completo usa cerca de 8 GB de RAM)
- .NET SDK 10.0.400 ou mais recente (para rodar testes ou depurar pela IDE)

## Como subir

```bash
docker compose up -d --build
```

A ordem de subida é controlada pelo compose: Postgres e Kafka, criação dos databases e dos tópicos com 3
partições, Kafka Connect, registro dos conectores, APIs de Contas (aplica as migrations e cria as contas de
exemplo Ana, Bruno e Carla) e de Transferências (aplica as migrations), e depois os workers (o de notificações
aplica as próprias migrations), os simuladores e o Mailpit.

| Serviço | URL |
|---|---|
| API de Contas (Swagger) | http://localhost:5100/swagger |
| Extrato (Swagger) | http://localhost:5200/swagger |
| API de Transferências (Swagger) | http://localhost:5300/swagger |
| Kafbat UI (tópicos, mensagens, consumer groups, conectores) | http://localhost:8080 |
| Kafka Connect (REST) | http://localhost:8083/connectors |
| Jaeger (traces) | http://localhost:16686 |
| Mailpit (e-mails das notificações) | http://localhost:8025 (SMTP em `localhost:1025`; portas configuráveis em `.env`) |
| PostgreSQL | `localhost:5434`, usuário e senha `eurobank` (porta configurável em `.env`: `POSTGRES_PORTA_HOST`) |
| Kafka (para apps no host) | `localhost:9092` |

As credenciais existem só para o ambiente local do laboratório. Se outro projeto já ocupa 8025 ou 1025 (um
segundo Mailpit, por exemplo), defina `MAILPIT_PORTA_UI` e `MAILPIT_PORTA_SMTP` no `.env`.

As rotas de negócio exigem o header `X-Api-Key`, com uma chave por API, definidas no `.env`
(`CONTAS_API_KEY`, `EXTRATO_API_KEY`, `TRANSFERENCIAS_API_KEY`). Sem a chave, a resposta é 401. O `/health` e o
Swagger ficam abertos; no Swagger, use o botão Authorize. Os roteiros `.http` e o k6 já enviam as chaves de
laboratório.

Roteiros de requisições prontos: [`http/contas.http`](http/contas.http), [`http/extrato.http`](http/extrato.http)
e [`http/transferencias.http`](http/transferencias.http).

Para recomeçar do zero (apaga os dados do laboratório): `docker compose down -v`.

## Estrutura

```
src/
  Shared/   EuroBank.Shared (blocos de domínio, Dinheiro e Moeda, Paginacao)
            EuroBank.Shared.Persistence (outbox genérica com escrita ordenada)
            EuroBank.Shared.Messaging (mapeador do Debezium e convenções do Wolverine)
            EuroBank.Shared.Web (Serilog, OpenTelemetry, erros em ProblemDetails, API key)
  Contas/   Domain | Application | Infrastructure | Api | Worker (comandos das sagas)
  Extrato/  Domain | Application | Infrastructure | Worker
  Transferencias/  Domain | Application | Infrastructure | Api | Worker (orquestrador das sagas)
  Notificacoes/  Worker (e-mails dos eventos finais das sagas; um projeto só, com as pastas técnicas)
  Simuladores/  Comum | BancoExterno | Cambio (sistemas de fora: Confluent.Kafka e Npgsql puros, sem Wolverine e sem EF)
tests/      testes unitários, de integração (Testcontainers) e ponta a ponta (Debezium de verdade)
infra/      criação dos databases e dos tópicos, conectores do Debezium, script do k6
http/       roteiros de requisições
```

Convenções: nomes de projetos e pastas técnicas em inglês (`Entities`, `ValueObjects`, `Events`, `Exceptions`,
`TransferObjects`, `Replies`...), classes de negócio e métodos em português, uma classe por arquivo. Os
contextos não referenciam uns aos outros: cada um tem os próprios DTOs para as mensagens que consome.

Pontos do código que valem a leitura:

- `src/Contas/EuroBank.Contas.Domain/Entities/Conta.cs`: o agregado decide qual evento acontece.
- `src/Shared/EuroBank.Shared.Persistence/Outbox/UnidadeDeTrabalhoComOutbox.cs`: estado e outbox na mesma
  transação, com as linhas inseridas na ordem dos eventos.
- `infra/connect/conectores/eurobank-contas-outbox.json`: como cada coluna da outbox vira tópico, chave, header
  ou corpo da mensagem.
- `src/Shared/EuroBank.Shared.Messaging/MapeadorEnvelopeDebezium.cs`: como o Wolverine lê os
  headers do Debezium.
- `src/Shared/EuroBank.Shared.Messaging/WolverineExtensions.cs`: inbox durável, ordem por
  conta, retry e dead letter.
- `src/Extrato/EuroBank.Extrato.Domain/Entities/ExtratoConta.cs`: a regra de versão que torna a projeção
  idempotente.
- `src/Transferencias/EuroBank.Transferencias.Domain/Entities/Transferencia.cs`: a saga como agregado; cada
  resposta e cada prazo vencido decide o próximo comando, e o que não cabe no estado atual é ignorado e anotado
  no histórico. A confirmação tardia, a reconciliação e a devolução estão no mesmo agregado.
- `src/Transferencias/EuroBank.Transferencias.Domain/ValueObjects/PoliticaSepa.cs`: o horário de corte SEPA e o
  próximo dia útil.
- `src/Transferencias/EuroBank.Transferencias.Infrastructure/Persistence/Repositories/RepositorioTransferencias.cs`:
  o advisory lock do limite diário e o `FOR UPDATE SKIP LOCKED` da varredura.
- `src/Simuladores/EuroBank.Simuladores.Comum/RepositorioSimulador.cs` e
  `src/Simuladores/EuroBank.Simuladores.BancoExterno/ProcessadorComandos.cs`: inbox e agenda de respostas
  escritas à mão, para comparar com o que o Wolverine faz nos outros serviços.
- `src/Simuladores/EuroBank.Simuladores.Cambio/Regras/TabelaCambio.cs`: taxas cruzadas pelo euro, spread e
  arredondamento meio para cima.
- `src/Notificacoes/EuroBank.Notificacoes.Worker/Services/ServicoNotificacoes.cs` e
  `src/Notificacoes/EuroBank.Notificacoes.Worker/Messaging/ConfiguracaoMensageria.cs`: a deduplicação pelo id do
  evento e as novas tentativas agendadas quando o SMTP falha.
- `infra/k6/carga.js`: o balanço por moeda que confere, no fim da carga, que nenhum dinheiro sumiu.

## Depurar pela IDE

A infra continua no Docker; só as aplicações rodam no host.

```bash
docker compose stop contas-api extrato-worker transferencias-api transferencias-worker notificacoes-worker
dotnet run --project src/Contas/EuroBank.Contas.Api
dotnet run --project src/Extrato/EuroBank.Extrato.Worker
dotnet run --project src/Transferencias/EuroBank.Transferencias.Api
dotnet run --project src/Transferencias/EuroBank.Transferencias.Worker
dotnet run --project src/Notificacoes/EuroBank.Notificacoes.Worker
```

Os `appsettings.json` já apontam para `localhost:5434` (Postgres) e `localhost:9092` (Kafka), e as portas
continuam 5100, 5200 e 5300 (os workers de Contas e de Transferências ficam em 5101 e 5301, o de notificações
em 5501, o banco externo simulado em 5401 e o câmbio simulado em 5402). O worker de notificações manda os e-mails
para `localhost:1025`, o SMTP do Mailpit.

## Testes

```bash
dotnet test
```

Precisa do Docker rodando: os testes de integração e ponta a ponta sobem os próprios containers com
Testcontainers. Os testes ponta a ponta usam os mesmos conectores de `infra/connect/conectores` e cobrem o
evento nos dois tópicos, a convergência do extrato, o trace contínuo, o Kafka Connect parado e religado sem
perda, a duplicata barrada pela inbox e as mensagens inválidas no dead letter. Para a saga, com prazos curtos,
cobrem cada final de IBAN do banco externo, as recusas (saldo, conta bloqueada ou encerrada, limite diário), o
agendamento, os dois tipos de cancelamento, a `Idempotency-Key` e a trava que impede encerrar a conta. Para o
câmbio: a externa em outra moeda com o extrato mostrando a cotação, o par indisponível, o câmbio entre contas
EuroBank (concluído e com o destino encerrado) e a transferência síncrona entre moedas recusada. Para a fase de
SEPA e devolução: a confirmação tardia que leva a `Divergente`, a reconciliação que deixa a conta negativa (e a
transferência seguinte recusada por `SaldoNegativo`) e a devolução que credita o valor sem a tarifa; o horário
de corte é testado com o relógio controlado. Para as notificações: o e-mail de uma transferência concluída e de
uma recusada no Mailpit, nenhum e-mail sem `emailNotificacao`, e o tópico relido por outro consumer group, com a
inbox apagada, sem nenhum e-mail repetido. Os testes do worker cobrem os modelos em pt-BR (inclusive o escape do
HTML), a deduplicação, as novas tentativas agendadas até o dead letter e o envio pelo MailKit para um Mailpit de
verdade.

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
SEPA_HORARIO_DE_CORTE=false docker compose up -d transferencias-api
docker compose --profile carga run --rm k6
docker compose --profile carga run --rm -e VUS=20 -e DURACAO=2m k6
docker compose --profile carga run --rm -e FINAIS_LENTOS=true k6
```

O script abre contas nas quatro moedas e mistura depósitos, saques, transferências internas, externas (finais
`00`, `99`, `97`, `96` e `94`; com `FINAIS_LENTOS=true`, também `98` e `95`, e a espera passa de 3 para 7
minutos), externas em outra moeda e câmbio entre contas, um quinto delas com `emailNotificacao` (os e-mails
aparecem no Mailpit). No fim:

1. espera todas as sagas chegarem ao estado final (o final `94` só termina quando é devolvido; o `95`, quando
   fica `Divergente`);
2. confere conta a conta se o Extrato convergiu para o saldo e a versão de Contas;
3. fecha o balanço de cada moeda: a soma dos saldos tem de ser igual aos depósitos menos os saques, menos o que as
   sagas tiraram de vez das contas (valor e tarifa das concluídas, só a tarifa das devolvidas), mais o que o câmbio
   creditou.

O corte SEPA precisa estar desligado: com ele, as externas em euro feitas depois das 16:00 UTC ou no fim de
semana ficariam agendadas para o próximo dia útil. O k6 confere isso antes de gerar carga, com uma transferência
de 1 EUR, e aborta com a instrução se ela ficar `Agendada`.

Numa máquina de desenvolvimento, 10 usuários virtuais durante 1 minuto fizeram cerca de 140 operações por segundo
e iniciaram 1.926 sagas: 1.504 concluídas, 134 canceladas, 130 devolvidas e 158 recusadas. Todas chegaram ao
estado final, as contas convergiram no Extrato, as quatro moedas fecharam ao centavo, e cada um dos 408 e-mails da
carga chegou uma vez ao Mailpit.

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

### 9. Um caminho da saga por final de IBAN

Pelo `http/transferencias.http`, peça transferências para IBANs terminados em `00`, `99`, `98`, `97`, `96`, `95` e
`94` (uma `Idempotency-Key` diferente em cada pedido) e acompanhe cada uma pelo `GET /transferencias/{id}`: o histórico
mostra cada comando enviado, cada reenvio, cada resposta recebida e as respostas ignoradas. No Kafbat UI, siga
os comandos e as respostas pelos tópicos `eurobank.contas.*` e `eurobank.banco-externo.*`. Os finais `98` e `96`
levam cerca de 2 minutos com os prazos padrão; para encurtar:

```bash
SAGA_PRAZO_LIQUIDACAO=00:00:05 SAGA_PRAZO_CONSULTA=00:00:03 docker compose up -d transferencias-api transferencias-worker
```

### 10. Contas fora do ar no meio da saga

```bash
docker compose stop contas-worker
# peça uma transferência pelo transferencias.http e acompanhe o histórico
docker compose start contas-worker
```

Sem resposta, a saga reenvia o débito três vezes e termina `Recusada` (`SemRespostaDeContas`). Os comandos
continuam no Kafka: quando o worker de Contas volta, ele debita uma vez só (os reenvios têm o mesmo id de
transferência) e responde. A saga recebe o `DebitoRealizado` tardio, pede o estorno e termina `Cancelada`, com o
saldo de volta.

### 11. Duas instâncias do orquestrador

```bash
docker compose up -d --scale transferencias-worker=2
docker compose logs -f transferencias-worker
```

As duas instâncias dividem as partições dos tópicos de resposta e rodam a varredura ao mesmo tempo. O
`FOR UPDATE SKIP LOCKED` faz cada transferência vencida ser tratada por uma instância só. Para voltar a uma:
`docker compose up -d --scale transferencias-worker=1`.

### 12. Câmbio: para fora e entre contas

Pelo `http/contas.http`, abra uma conta em USD; pelo `http/transferencias.http`, mande 100 USD de uma conta em
euro para um IBAN externo (`moedaDestino` e `moedaDoValor` iguais a `USD`) e para a conta em dólar
(`POST /transferencias-cambio`). No `GET /transferencias/{id}`, veja a cotação travada, o valor debitado em euro
e o valor de destino em dólar; no Kafbat UI, o `CotarCambio` em `eurobank.cambio.comandos` e a resposta em
`eurobank.cambio.respostas`. Os extratos das duas contas mostram os dois lados, cada um na sua moeda. Para ver a
recusa por câmbio, abra uma conta em BRL e mande para fora com `moedaDestino` igual a `GBP`.

### 13. Divergência e reconciliação

Pelo `http/transferencias.http`, abra uma conta com 102,50 EUR e mande 100 EUR para um IBAN terminado em `95`.
A saga estorna e, quando o banco confirma, fica `Divergente`. Saque parte do dinheiro estornado e peça a
reconciliação: a conta fica negativa, o extrato mostra o `DebitoReconciliacao` com o saldo negativo, e a próxima
transferência dessa conta termina `Recusada` (`SaldoNegativo`). Um depósito que cubra o negativo libera a conta.
A confirmação do final `95` leva 5 minutos; para encurtar, reduza também os prazos da saga, porque a confirmação
precisa chegar depois do estorno (com os prazos padrão, a saga estorna em cerca de 2 minutos; com estes, em
cerca de 25 segundos):

```bash
SAGA_PRAZO_LIQUIDACAO=00:00:05 SAGA_PRAZO_CONSULTA=00:00:03 docker compose up -d transferencias-api transferencias-worker
BANCO_EXTERNO_ATRASO_95=40 docker compose up -d banco-externo-simulador
```

### 14. Devolução e horário de corte

Mande 100 EUR para um IBAN terminado em `94`: a saga conclui e, 10 segundos depois, termina `Devolvida`, com os
100 EUR de volta e a tarifa cobrada. No Kafbat UI, siga a `TransferenciaDevolvida` em
`eurobank.banco-externo.respostas` e o `CreditarDevolucao` em `eurobank.contas.comandos`. Depois, com o corte SEPA
ligado (o padrão), peça uma transferência em euro depois das 16:00 UTC ou no fim de semana: ela fica `Agendada`,
com `executarEmEfetivo` no próximo dia útil às 08:00 UTC. Para desligar o corte:

```bash
SEPA_HORARIO_DE_CORTE=false docker compose up -d transferencias-api
```

### 15. Cancelamento e agendamento

Pelo `http/transferencias.http`, peça uma externa com `executarEm` no futuro: ela fica `Agendada`, nada é
debitado, e a varredura do worker só começa a saga quando `executarEmEfetivo` chega (saldo e limite são
conferidos nessa hora). Cancele: termina `Cancelada` (`CanceladaPeloCliente`) sem passar por Contas.

Para ver o cancelamento com estorno, pare o worker de Contas, peça uma transferência e cancele logo em seguida: a
saga só anota o pedido, porque o débito já está a caminho. Religue o worker em menos de 30 segundos (antes de o
prazo do débito vencer):

```bash
docker compose stop contas-worker
# peça a transferência e o cancelamento pelo transferencias.http
docker compose start contas-worker
```

O `DebitoRealizado` chega, a saga pede o estorno e termina `Cancelada`, com o saldo de volta e o débito e o
estorno no extrato. Depois que a liquidação foi pedida, o cancelamento responde 409: o dinheiro pode já estar no
outro banco.

### 16. Limite diário

Use uma conta com pelo menos 5.100 EUR (deposite antes pelo `http/contas.http`) e rode os dois últimos pedidos do
`http/transferencias.http`: 4.950 EUR e depois 100 EUR. A segunda termina `Recusada` (`LimiteDiarioExcedido`) sem
chegar a Contas, e a tarifa não conta para o limite. O limite é por conta, na moeda dela (tabela em
[Transferências com câmbio](#transferências-com-câmbio)), e por dia UTC da execução; transferências canceladas,
recusadas, divergentes ou devolvidas não ocupam o limite.

### 17. Notificações por e-mail

Peça uma transferência com `emailNotificacao` (o primeiro pedido do `http/transferencias.http` já tem) e abra o
Mailpit em http://localhost:8025: o e-mail chega quando a saga termina, com as versões HTML e texto e o
`Message-ID` igual ao id do evento. Para ver o SMTP fora do ar:

```bash
docker compose stop mailpit
# peça uma transferência com emailNotificacao e espere ela terminar
docker compose exec postgres psql -U eurobank -d eurobank_notificacoes -c "SELECT tipo_evento, status, tentativas, ultimo_erro FROM notificacoes ORDER BY criada_em DESC LIMIT 5;"
docker compose start mailpit
```

A notificação fica `Pendente`, com as tentativas e o erro, e a saga não espera por ela. Religando o Mailpit em até
2 minutos e meio, a próxima tentativa agendada entrega o e-mail; passado esse tempo, a mensagem vai para o dead
letter (`wolverine.wolverine_dead_letters`, no mesmo banco). Para reprocessar o tópico:

```bash
docker compose stop notificacoes-worker
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server kafka:29092 \
  --group eurobank-notificacoes --topic eurobank.transferencias.eventos --reset-offsets --to-earliest --execute
docker compose exec postgres psql -U eurobank -d eurobank_notificacoes -c "DELETE FROM wolverine.wolverine_incoming_envelopes;"
docker compose start notificacoes-worker
```

Nenhum e-mail se repete, mesmo com a inbox apagada: o log do worker mostra `já notificado ... ignorado`, porque a
tabela `notificacoes` guarda o id de cada evento já notificado.

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
| Uma API key fixa por API, guardada no `.env` | OAuth 2.0 ou OpenID Connect com tokens de curta duração e segredos em cofre |
| Banco externo simulado, com o comportamento escolhido pelo final do IBAN | Integração com a rede de pagamentos (SEPA, mensagens ISO 20022), com contrato e homologação |
| Prazo fixo por passo e até 3 reenvios | Espera crescente com variação aleatória e alerta para sagas paradas num estado |
| Orquestrador próprio, com varredura a cada 2 s | Igual, ou um motor de workflow (Temporal, Camunda) quando os processos são muitos e longos |
| Câmbio simulado com taxas fixas e spread único | Provedor de cotações real, com taxas que mudam a cada segundo e spread por cliente e por valor |
| Projeção local de contas montada só pelos eventos que o worker viu | Tópico com retenção longa ou compactado, ou um snapshot inicial da projeção antes de consumir |
| Horário de corte fixo (16:00 UTC, sem feriados) e lista fixa de países SEPA | Calendário TARGET2 com feriados, cortes por esquema (SCT, SCT Inst) e dados de referência mantidos à parte |
| Reconciliação pedida pela API, sem aprovação e sem limite para o negativo | Fila de exceções operacionais com aprovação, limite de crédito e cobrança do saldo negativo |
| Mailpit aceita qualquer e-mail, sem TLS e sem autenticação | Provedor de e-mail com TLS, SPF, DKIM e DMARC, tratamento de bounces e preferências de comunicação do cliente |
| E-mail do cliente dentro do evento no Kafka | Dados pessoais fora dos eventos (o consumidor busca por id num serviço com controle de acesso), ou tópico com retenção curta e criptografia |

## Próximos passos

Prontas: a **saga orquestrada** de transferência externa (prazos, reenvios, consulta antes de estornar,
cancelamento, agendamento, tarifa, limite diário e `Idempotency-Key`), o **câmbio** (contas em quatro moedas,
cotação travada com spread, externa em outra moeda e câmbio entre contas EuroBank) e as regras **SEPA e SWIFT**
com horário de corte, a **devolução** pelo banco externo, o estado **`Divergente`** com reconciliação e saldo
negativo controlado, as **notificações** por e-mail e o **cenário de carga** que confere o dinheiro de todas as
sagas.

A próxima: um contexto de **antifraude** com regras e machine learning (ML.NET), comunicação gRPC entre os
serviços e Redis.
