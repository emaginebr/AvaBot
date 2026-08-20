# Research: Área Administrativa Bot Telegram

**Date**: 2026-04-11  
**Feature**: 009-telegram-bot-admin

## 1. Backend API Endpoints Disponíveis

**Decision**: Utilizar os 3 endpoints Telegram autenticados já existentes no backend.

**Rationale**: O backend em C:\repos\AvaBot\AvaBot já implementa toda a lógica de webhook Telegram. Não é necessário nenhum novo endpoint.

**Endpoints confirmados**:

| Endpoint | Método | Auth | Resposta |
|----------|--------|------|----------|
| `/telegram/{id}/setup-webhook` | POST | Sim | `Result<TelegramWebhookInfo>` |
| `/telegram/{id}/webhook-info` | GET | Sim | `Result<TelegramWebhookInfo>` |
| `/telegram/{id}/regenerate-secret` | POST | Sim | `Result<TelegramWebhookInfo>` |

**TelegramWebhookInfo DTO**:
- `agentId` (long)
- `agentSlug` (string)
- `webhookUrl` (string | null)
- `isConfigured` (boolean)

**Alternatives considered**: Criar endpoints específicos para salvar campos Telegram separadamente — rejeitado pois o `PUT /agents/{id}` já suporta atualização dos campos TelegramBotName e TelegramBotToken via AgentInsertInfo.

## 2. Campos Telegram no Agent

**Decision**: Reutilizar os campos já existentes no modelo Agent do backend.

**Rationale**: O Agent já possui `TelegramBotName`, `TelegramBotToken` e `TelegramWebhookSecret`. O AgentInfo DTO já retorna esses campos. O AgentInsertInfo aceita `TelegramBotName` e `TelegramBotToken` no update.

**Nota**: O `TelegramWebhookSecret` é gerenciado exclusivamente pelo backend (gerado automaticamente, não enviado no insert/update). O frontend apenas exibe o valor retornado no AgentInfo.

## 3. Padrão de Página Admin no Frontend

**Decision**: Seguir o padrão existente de AgentSettingsPage.tsx.

**Rationale**: A página de configurações do bot Telegram é similar em estrutura à página de configurações do agente — exibe dados do agente selecionado e permite ações. O padrão inclui:
- Hook `useAgentStore` para obter agente selecionado
- `AgentService` para chamadas API
- Toast notifications via `sonner`
- Layout com cards (`bg-white rounded-xl border border-gray-200`)
- Botões de ação com estados de loading

**Alternatives considered**: Criar uma seção dentro de AgentSettingsPage — rejeitado pois o usuário especificou uma área separada no menu.

## 4. Tipos TypeScript Faltantes no Frontend

**Decision**: Adicionar `TelegramWebhookInfo` interface e campos Telegram em `AgentInfo`.

**Rationale**: O frontend `types/agent.ts` ainda não inclui os campos Telegram no AgentInfo nem o tipo TelegramWebhookInfo. Precisam ser adicionados para tipar corretamente as respostas dos endpoints.

## 5. Validação do Nome do Bot

**Decision**: Validação client-side com regex `/bot$/i` antes de salvar.

**Rationale**: O Telegram exige que nomes de bot terminem com "bot". A validação no frontend dá feedback imediato ao usuário. O backend não valida isso explicitamente.

**Alternatives considered**: Validação apenas server-side — rejeitado para melhor UX com feedback instantâneo.

## 6. Persistência dos Dados Telegram

**Decision**: Usar `PUT /agents/{id}` existente para salvar TelegramBotName e TelegramBotToken.

**Rationale**: O endpoint de update do agente já aceita esses campos no AgentInsertInfo. Após salvar, recarregar o agente do store para obter o TelegramWebhookSecret gerado.

**Flow**: Salvar → PUT /agents/{id} → loadAgents() → exibir dados atualizados (incluindo secret).
