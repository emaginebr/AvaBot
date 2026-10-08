# Tasks: Diagnóstico completo e retentativas Power BI

**Input**: Design documents from `specs/014-powerbi-error-retry/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/powerbi-query-error.md`

**Test tasks**: Não incluídas. A spec descreve critérios de aceitação, mas não pede TDD nem solicita explicitamente tarefas de implementação de testes automatizados.

**Organization**: Tarefas agrupadas por história do usuário; fundamentos compartilhados vêm antes das histórias.

## Phase 1: Setup

**Purpose**: A solução .NET e o frontend já estão inicializados; nenhuma instalação ou scaffold é necessário.

Sem tarefas de setup.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Preservar e reconhecer os erros do serviço antes de implementar retentativas, retorno ao modelo e histórico integral.

- [X] T001 Expandir `PowerBIApiException` para carregar status HTTP, código, diagnóstico integral, corpo original e `Retry-After` em `AvaBot.Infra.Interfaces/AppServices/IPowerBIClient.cs`
- [X] T002 Atualizar tratamento de resposta para extrair erros nos níveis `error`, `results[].error` e `tables[].error`, preservar corpo não reconhecido e identificar erros em resposta HTTP 200 em `AvaBot.Infra/AppServices/PowerBIClient.cs`

**Checkpoint**: Cliente Power BI disponibiliza informação completa e classificada às camadas superiores.

---

## Phase 3: User Story 1 - Consultar BI com recuperação automática de erros (Priority: P1) 🎯 MVP

**Goal**: Devolver erros de DAX ao modelo para correção, repetir falhas temporárias automaticamente com a mesma DAX e encerrar sem exceder cinco execuções padrão.

**Independent Test**: Simular uma DAX rejeitada seguida de DAX corrigida bem-sucedida e uma falha temporária seguida de replay idêntico; conferir que o modelo recebe detalhes antes da correção e que cada execução respeita o limite.

### Implementation for User Story 1

- [X] T003 [US1] Implementar orçamento por mensagem com `MaxQueryAttempts` (fallback 5), contador que exclui `listar_schema`, replay automático da mesma DAX para 429/5xx/timeout/rede, uso de `Retry-After` e backoff exponencial com jitter em `AvaBot.Application/Services/PowerBIToolProvider.cs`
- [X] T004 [US1] Devolver diagnóstico integral estruturado ao modelo em falhas de DAX, interromper chamadas após esgotar orçamento e atualizar `PromptAddendum` para corrigir DAX com base no erro em `AvaBot.Application/Services/PowerBIToolProvider.cs`
- [X] T005 [P] [US1] Ajustar o limite efetivo de chamadas do loop OpenAI para permitir as tentativas BI configuradas além da leitura auxiliar de schema, mantendo o bloqueio do executor, em `AvaBot.Application/Services/ChatService.cs` e `AvaBot.Infra/AppServices/OpenAIService.cs`

**Checkpoint**: Histórias DAX corrigível e retry transitório funcionam no chat streaming e não streaming sem chamada Power BI após o limite.

---

## Phase 4: User Story 2 - Consultar erros completos no Histórico do Power BI (Priority: P1)

**Goal**: Associar o diagnóstico integral a cada tentativa no histórico administrativo junto à pergunta e DAX correspondentes.

**Independent Test**: Produzir mensagem de erro com mais de 2.000 caracteres e detalhes aninhados; verificar que a persistência, endpoint paginado e histórico administrativo apresentam o conteúdo completo sem divulgar segredos.

### Implementation for User Story 2

- [X] T006 [P] [US2] Alterar o mapeamento de `PowerBIQueryLog.ErrorMessage` para `text` e gerar migração EF Core que converte `error_message` de `varchar(2000)` para `text` preservando registros em `AvaBot.Infra/Context/AvaBotContext.cs` e `AvaBot.Infra/Migrations/`
- [X] T007 [US2] Remover truncamento de 2.000 caracteres do erro persistido e da resposta de teste do agente, gravar corpo/diagnóstico integral por falha e preservar redaction de segredos em `AvaBot.Application/Services/PowerBIToolProvider.cs`

**Checkpoint**: Cada chamada HTTP falha tem uma linha de histórico integral; o campo atual `errorMessage` continua compatível com API e UI.

---

## Phase 5: User Story 3 - Configurar o limite de tentativas (Priority: P2)

**Goal**: Disponibilizar o limite padrão de cinco tentativas na configuração de aplicação para operadores.

**Independent Test**: Definir `PowerBI:MaxQueryAttempts` abaixo de cinco e verificar que o contador usa esse valor; omitir ou invalidar o valor e verificar fallback cinco.

### Implementation for User Story 3

- [X] T008 [P] [US3] Adicionar `MaxQueryAttempts` com padrão `5` na seção `PowerBI` de `AvaBot.API/appsettings.json`

**Checkpoint**: O limite do executor é configurável em appsettings e valores ausentes/inválidos permanecem seguros com fallback positivo.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentar o novo comportamento operacional para manutenção e configuração.

- [X] T009 Atualizar a documentação de integração Power BI com `PowerBI:MaxQueryAttempts`, classificação de erros, contagem por mensagem e tratamento de `Retry-After` em `docs/POWERBI_INTEGRATION.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Nenhuma tarefa; solução e dependências já existem.
- **Foundational (Phase 2)**: T001 → T002; bloqueia as histórias porque todas precisam do diagnóstico classificado e preservado.
- **User Stories (Phase 3+)**: Começam após T002. User Story 1 é MVP; US2 depende também do fluxo completo do executor para persistir cada falha; US3 depende do leitor de configuração criado em US1.
- **Polish (Phase 6)**: T009 segue depois das histórias para documentar o comportamento entregue.

### User Story Dependencies

```text
T001 → T002 → US1 (T003 → (T004 ∥ T005))
                  ├─ US2 (T006 paralelo; T007 após T004)
                  └─ US3 (T008 pode ocorrer em paralelo após T003)
US1 + US2 + US3 → T009
```

- **US1 (P1)**: Depende apenas do fundamento e entrega recuperação automática.
- **US2 (P1)**: Depende do formato completo definido no fundamento e da produção do diagnóstico em US1; a alteração de coluna T006 não conflita com os arquivos de US1 e pode ser preparada em paralelo.
- **US3 (P2)**: O código já lê a configuração/fallback em T003; T008 publica o valor padrão no `appsettings` e pode ser feito em paralelo com T006.

### Parallel Opportunities

- Após T002, T006 (`AvaBotContext.cs`/migração) pode ser desenvolvido em paralelo com T003–T005 (`PowerBIToolProvider.cs`, `ChatService.cs`, `OpenAIService.cs`) por atuar em arquivos distintos.
- Após T003, T008 (`AvaBot.API/appsettings.json`) pode ser feito em paralelo com T006 e T007.
- T003 e T004 não são paralelos porque alteram o mesmo `PowerBIToolProvider.cs` e compartilham o contador/fluxo de erro.
- T005 depende de T003 para coordenar o teto geral com o orçamento específico de consultas.

## Parallel Example: User Story 2

```text
# Enquanto US1 implementa orçamento e retries, prepare a persistência em arquivos distintos:
T006: Atualizar mapeamento EF e gerar migração de error_message para text em AvaBot.Infra/Context/AvaBotContext.cs e AvaBot.Infra/Migrations/

# Depois da integração do diagnóstico em US1:
T007: Remover truncamento e persistir diagnóstico integral por falha em AvaBot.Application/Services/PowerBIToolProvider.cs
```

## Parallel Example: User Story 1

```text
# Depois de T003, estes trabalhos usam arquivos distintos:
T004: Devolver diagnóstico DAX e ajustar PromptAddendum em AvaBot.Application/Services/PowerBIToolProvider.cs
T005: Ajustar teto do loop em AvaBot.Application/Services/ChatService.cs e AvaBot.Infra/AppServices/OpenAIService.cs
```

## Parallel Example: User Story 3

```text
# Depois de T003, adicionar o valor publicado enquanto a migração/histórico avança:
T008: Declarar MaxQueryAttempts=5 em AvaBot.API/appsettings.json
T006: Converter error_message para text em AvaBot.Infra/Context/AvaBotContext.cs e AvaBot.Infra/Migrations/
```

## Implementation Strategy

### MVP First (User Story 1)

1. Concluir T001–T002 para normalizar diagnósticos completos do cliente Power BI.
2. Concluir T003–T005 para retries transitórios, correção de DAX pelo modelo e orçamento independente.
3. Validar a história com os cenários independentes descritos acima antes de adicionar o histórico integral.

### Incremental Delivery

1. Foundation + US1: recuperação de consulta com até cinco execuções padrão.
2. US2: persistir e exibir mensagens completas no histórico, incluindo erros maiores que 2.000 caracteres.
3. US3: publicar/ajustar o limite no appsettings.
4. Polish: atualizar documentação da integração.

## Notes

- `[P]` indica tarefas em arquivos diferentes e sem dependências pendentes.
- Não há tarefas de teste automatizado nesta lista; os critérios de aceitação e cenários de validação permanecem descritos acima e em `spec.md`/`quickstart.md`.
- Cada retry temporário consome uma execução do orçamento, grava sua própria falha e repete a mesma DAX; cada DAX de correção gerada pelo modelo também consome uma execução.

## Status de execução (2026-10-08)

T001–T009 concluídas.

| Tarefa | O que ficou |
|---|---|
| T001 | `PowerBIApiException` ganha `ResponseBody` e `RetryAfter` (_ctor_ com parâmetros opcionais no fim, então as chamadas existentes continuam válidas). |
| T002 | `BuildApiException`: mensagem principal **+** todos os textos `message`/`value` aninhados (o código antigo **sobrescrevia** a principal pelo detalhe); corpo integral preservado e redigido; `Retry-After` numérico e HTTP-date; corpo não reconhecido vira diagnóstico em vez de mensagem genérica. Novo `BodyReportsError` varre raiz, `results[].error` e `results[].tables[].error` — um 200 com erro estruturado agora falha. Mesma preservação/redação aplicada ao erro do token no Entra ID. |
| T003/T004 | Orçamento por toolset (`_queryAttempts`, `MaxQueryAttempts`, fallback 5), replay da mesma DAX para 429/5xx/timeout/rede com `Retry-After` ou backoff (~1 s, dobrando, teto 30 s, jitter), `listar_schema` fora da conta, bloqueio após esgotar, categorias `dax_query`/`transient_service`/`authentication_or_permission`/`attempt_limit`, diagnóstico estruturado no `ToolChatMessage` e `PromptAddendum` ensinando a corrigir a DAX com o erro (a instrução antiga mandava parar em qualquer falha). |
| T005 | `ChatService` usa `Math.Max(MaxToolCallsPerMessage, toolset.ModelToolCallBudget)` — com os padrões, 6 chamadas no loop (5 de consulta + 1 de schema) — no streaming e no teste. **`OpenAIService` não precisou de alteração**: o teto já é parâmetro e o bloqueio do executor fica no provider; alterar lá seria inventar mudança. |
| T006 | `error_message` mapeada como `text`; migração `20261008214505_ChangePowerBIQueryLogErrorMessageToText` (só `AlterColumn`, preserva linhas) + par `scripts/powerbi-error-message-to-text.sql`. |
| T007 | Sem truncamento em `Sanitize`, na prévia do teste do agente e no `ErrorMessage` persistido; redação agora alcança também o `responseBody` (o teste novo pegou que ele passava cru). |
| T008 | `PowerBI:MaxQueryAttempts: 5` em `appsettings.json`. |
| T009 | `docs/POWERBI_INTEGRATION.md`: chave nova, classificação por categoria, contagem por mensagem, `Retry-After`/backoff, histórico integral; removida a afirmação falsa de que não havia retry automático. |

Verificação: `dotnet build AvaBot.sln` 0 erros; `dotnet test AvaBot.Tests` **145 aprovados, 0 falhas** (141 anteriores — 4 adaptadas ao contrato de erro estruturado — + 4 novas: replay da mesma DAX, corte no orçamento, schema fora da conta e diagnóstico > 2.000 caracteres persistido). Frontend intocado: `PowerBIQueryLogTable.tsx` já renderiza `errorMessage` cru com `whitespace-pre-wrap`.

Não executado: nada contra Power BI real nem Postgres (sem servidor de banco na sessão). O caminho de backoff com `Retry-After > 0` não é exercitado em teste — os testes usam `Retry-After: 0` para não dormir; e o downgrade da coluna falha se algum registro passou de 2.000 caracteres (anotado no script).
