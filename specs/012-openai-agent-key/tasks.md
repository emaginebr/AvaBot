# Tasks: Chave OpenAI por Agente

**Input**: Documentos de design em `/specs/012-openai-agent-key/`  
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/agents-openai.md`, `quickstart.md`

**Organização**: Tarefas agrupadas por história para permitir implementação incremental. A spec não solicita TDD nem criação de testes automatizados; por isso, esta lista não inclui tarefas para adicionar testes. Cada história mantém critérios de validação independente para conferência manual.

## Phase 1: Setup

**Purpose**: Preparação do repositório para a implementação.

O backend, frontend, dependências e infraestrutura de migrações já existem. Não são necessárias tarefas de inicialização de projeto.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Adicionar o armazenamento associado ao agente que será usado pela configuração e pelo diagnóstico.

- [X] T001 Adicionar `OpenAIApiKeyEncrypted` nullable ao agente, mapear a coluna `openai_api_key_encrypted` em snake_case e criar a migração EF Core com atualização do snapshot em `AvaBot.Domain/Models/Agent.cs`, `AvaBot.Infra/Context/AvaBotContext.cs` e `AvaBot.Infra/Migrations/`

**Checkpoint**: Persistência da credencial pronta sem expor o segredo em DTOs.

---

## Phase 3: User Story 1 - Configurar chave OpenAI do agente (Priority: P1) 🎯 MVP

**Goal**: Permitir configurar, preservar, substituir ou remover explicitamente a chave de cada agente e usá-la em todas as operações OpenAI relacionadas.

**Independent Test**: Configurar chaves diferentes para dois agentes, reabrir ambos, confirmar o estado configurado sem revelar os valores e confirmar que salvar outros campos preserva cada chave. Executar chat, busca e ingestão para agentes configurados e confirmar que cada operação usa a chave correspondente.

### Implementation for User Story 1

- [X] T002 [US1] Estender `AgentInsertInfo` com chave e sinalizador de remoção, expor somente `hasOpenAIApiKey` em `AgentInfo` e implementar em `AgentProfile` e `AgentService` as regras de preservar, proteger, substituir e remover a credencial em `AvaBot.DTO/AgentDTOs.cs`, `AvaBot.Application/Profiles/AgentProfile.cs` e `AvaBot.Application/Services/AgentService.cs`
- [X] T003 [P] [US1] Alterar `OpenAIService` para resolver e descriptografar a chave pelo agente, criar o cliente com a credencial resolvida, remover a dependência da chave global e ajustar o registro de DI para a dependência de repositório em `AvaBot.Infra/AppServices/OpenAIService.cs` e `AvaBot.Application/DependencyInjection.cs`
- [X] T004 [US1] Propagar o identificador do agente nas chamadas de completion, streaming e teste e garantir que chat síncrono, chat com ferramentas e teste usem a credencial do agente em `AvaBot.Application/Services/ChatService.cs` e `AvaBot.API/Controllers/AgentController.cs`
- [X] T005 [P] [US1] Passar o identificador do agente às operações de embeddings na busca e na ingestão de arquivos em `AvaBot.Application/Services/SearchService.cs` e `AvaBot.Application/Services/IngestionService.cs`
- [X] T006 [P] [US1] Atualizar tipos e chamadas de criação/edição para incluir a chave somente na requisição, consumir `hasOpenAIApiKey` sem carregar o segredo e remover o log do payload completo em `frontend/src/types/agent.ts` e `frontend/src/Services/AgentService.ts`
- [X] T007 [US1] Adicionar campo de chave OpenAI, indicador de chave salva e ação explícita de remoção na seção Modelo de IA, preservando o valor salvo quando o campo não for alterado em `frontend/src/components/admin/AgentForm.tsx` e `frontend/src/pages/admin/AgentFormPage.tsx`

**Checkpoint**: P1 funciona de ponta a ponta e não retorna nem registra a chave completa.

---

## Phase 4: User Story 2 - Diagnosticar conexão OpenAI (Priority: P2)

**Goal**: Testar a chave atual do formulário ou a chave salva, confirmar autenticação sem gerar conteúdo e apresentar o resultado em modal.

**Independent Test**: Na edição de um agente, testar uma chave válida, inválida e ausente; confirmar que o modal diferencia sucesso e falha, não inicia geração, usa a chave atual do formulário quando preenchida e nunca apresenta a credencial.

**Dependency**: Requer a persistência e o fluxo de credencial da User Story 1, para permitir que o diagnóstico use a chave salva quando o campo estiver oculto e inalterado.

### Implementation for User Story 2

- [X] T008 [US2] Criar DTOs de diagnóstico e endpoint autenticado `POST /agents/{id}/openai/diagnose` que use a chave enviada ou resolva a credencial salva e teste autenticação sem geração, com erros sanitizados em `AvaBot.DTO/AgentDTOs.cs`, `AvaBot.API/Controllers/AgentController.cs` e `AvaBot.Infra/AppServices/OpenAIService.cs`
- [X] T009 [P] [US2] Adicionar o método autenticado de diagnóstico ao cliente frontend, enviando a chave atual somente no corpo da requisição e sem registrá-la em `frontend/src/Services/AgentService.ts` e `frontend/src/types/agent.ts`
- [X] T010 [US2] Criar modal acessível com estados de carregamento, sucesso, falha e chave ausente, e conectá-lo ao botão Diagnóstico da seção Modelo de IA em `frontend/src/components/admin/OpenAIConnectionDiagnosticModal.tsx` e `frontend/src/components/admin/AgentForm.tsx`

**Checkpoint**: P2 valida autenticação em uma operação sem geração e comunica o resultado com segurança.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Remover configuração global obsoleta e completar a revisão de exposição de segredos.

- [X] T011 Remover `OpenAI:ApiKey` global das configurações após a troca para credenciais por agente, preservando a configuração do modelo de embeddings, em `AvaBot.API/appsettings.json`, `AvaBot.API/appsettings.Development.json`, `AvaBot.API/appsettings.Docker.json` e `AvaBot.API/appsettings.Production.json`
- [X] T012 Revisar os caminhos de logging e tratamento de erro nas operações de agente e OpenAI para confirmar que chaves em requisições, exceções e respostas do provedor nunca aparecem em logs ou mensagens visíveis em `AvaBot.API/Controllers/AgentController.cs`, `AvaBot.Application/Services/AgentService.cs`, `AvaBot.Infra/AppServices/OpenAIService.cs` e `frontend/src/Services/AgentService.ts`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Nenhuma inicialização necessária; o repositório já contém a estrutura de backend, frontend e migrações.
- **Foundational (Phase 2)**: T001 adiciona o armazenamento e bloqueia as histórias.
- **User Story 1 (Phase 3)**: Depende de T001; conclui persistência segura, formulário e consumo por agente.
- **User Story 2 (Phase 4)**: Depende da credencial e do fluxo de edição de T001–T007.
- **Polish (Phase 5)**: Depende das duas histórias para retirar a chave global sem deixar chamadas sem credencial em fluxos implementados.

### User Story Dependencies

- **US1 (P1)**: Começa após T001; entrega o MVP para configurar e usar uma chave por agente.
- **US2 (P2)**: Começa após US1, pois precisa testar também a chave já salva sem recebê-la no frontend.

### Parallel Opportunities

- T003 e T006 podem avançar em paralelo após T001, pois atuam em arquivos de backend e frontend distintos e seguem os contratos definidos.
- T004 e T005 podem avançar em paralelo após T003, pois cobrem consumidores OpenAI separados.
- T009 pode avançar em paralelo com T008 usando o contrato `POST /agents/{id}/openai/diagnose`; T010 depende de ambos.

## Parallel Example: User Story 1

```text
Após T001:
Trilha backend: T002 → T003 → (T004 e T005 em paralelo)
Trilha frontend: T006 → T007
Integração: concluir todas as tarefas US1 antes de iniciar US2
```

## Parallel Example: User Story 2

```text
Trilha backend: T008
Trilha frontend: T009 em paralelo com T008
Interface: T010 após T008 e T009
```

## Implementation Strategy

### MVP First (User Story 1)

1. Concluir T001 para persistência criptografada.
2. Concluir T002–T007 para configurar e consumir a chave individual em todos os fluxos OpenAI do agente.
3. Conferir o critério independente da US1 descrito nesta fase antes de iniciar o diagnóstico.

### Incremental Delivery

1. Entregar US1 como primeiro incremento funcional: configuração, persistência, isolamento e uso da chave.
2. Entregar US2: diagnóstico sem geração em modal.
3. Concluir remoção da configuração global e revisão de logging e erros em Polish.

## Status de execução (2026-10-08)

T001–T012 concluídas. Verificado nesta sessão:

- `dotnet build AvaBot.sln`: 0 erros.
- `dotnet test AvaBot.Tests`: **140 aprovados, 0 falhas** (nenhum teste novo criado — a spec não solicita testes; os 140 existentes foram ajustados às assinaturas novas de `IOpenAIService` e dos construtores de `AgentService`/`AgentController`).
- `npx tsc -b` e `npm run build` no frontend: ok.
- ESLint nos arquivos novos/alterados desta feature: 0 erros. `AgentForm.tsx` mantém 1 erro pré-existente (`set-state-in-effect` no `setFormData(initialData)`), que já estava no baseline e não foi tocado.
- `scripts/add-openai-api-key-to-agent.sql` criado como par manual da migração (convenção da pasta `scripts/`), conferido nome a nome contra o `.cs` da migração.

Pendente de execução em ambiente real (não foi possível rodar aqui):

- Aplicar a migração `AddOpenAIApiKeyToAgent` (o Postgres local estava inacessível) e definir `POWERBI_SECRET_ENCRYPTION_KEY`, que também protege a chave OpenAI.
- Critérios independentes de US1/US2 do `quickstart.md`: dois agentes com chaves separadas, preservação ao salvar outros campos, remoção explícita e o modal de diagnóstico contra a API real.
- **Impacto operacional não coberto por código**: sem fallback para `OpenAI:ApiKey`, todo agente já existente fica sem chat, busca e ingestão até receber chave própria no painel. `docker-compose.yml` e `docker-compose-prod.yml` ainda injetam `OpenAI__ApiKey: ${OPENAI_API_KEY}`, que passou a ser config morta — vale remover para não induzir o operador a erro.
