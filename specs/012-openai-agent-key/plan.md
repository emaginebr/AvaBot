# Implementation Plan: Chave OpenAI por Agente

**Branch**: `012-openai-agent-key` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-openai-agent-key/spec.md`

## Summary

Adicionar uma credencial OpenAI individual por agente, configurável na seção Modelo de IA do formulário administrativo. Persistir a credencial protegida, manter a chave salva quando o formulário não a altera, permitir remoção explícita e oferecer um diagnóstico autenticado que valide a chave sem gerar texto. Todas as chamadas OpenAI vinculadas ao agente — chat, streaming, embeddings de busca e ingestão — devem usar a credencial daquele agente.

## Technical Context

**Language/Version**: C# / .NET 9 (backend atual); TypeScript 6 / React 19 (frontend atual)  
**Primary Dependencies**: ASP.NET Core, Entity Framework Core 9, Npgsql, OpenAI .NET SDK 2.10.0, React, React Router 7, Vite 8  
**Storage**: PostgreSQL via `AvaBotContext`; credencial persistida criptografada associada ao agente  
**Testing**: xUnit nos projetos `AvaBot.Tests` e `AvaBot.Tests.API`; verificações frontend pelos scripts existentes no `frontend/package.json`  
**Target Platform**: Aplicação web com API ASP.NET Core e interface React  
**Project Type**: Aplicação web full-stack em projetos backend .NET em camadas e frontend SPA  
**Performance Goals**: Diagnóstico executa uma única verificação de autenticação, sem chamada de geração; não foi definido SLA específico  
**Constraints**: A chave não pode ser retornada em texto puro, exibida em respostas ou incluída em logs; operações administrativas e diagnóstico exigem autenticação; preservar a compatibilidade dos vetores de embeddings existentes  
**Scale/Scope**: Uma chave OpenAI opcional por agente; uma tela administrativa de criação/edição, operações OpenAI existentes e um fluxo de diagnóstico

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Evidência / decisão |
|---|---|---|
| Skills obrigatórias de arquitetura | Atendido no planejamento | A implementação deve seguir `dotnet-architecture` e `react-architecture`, conforme a constituição. Esses arquivos não estão presentes no catálogo local atual; confirmar/disponibilizar as skills antes de executar tarefas de implementação. |
| Stack e dependências | Atendido com divergência preexistente | O repositório usa .NET 9, React 19, React Router 7, TypeScript 6 e Vite 8, enquanto a constituição lista .NET 8, React 18, React Router 6, TypeScript 5 e Vite 6. Este plano mantém as versões efetivamente usadas e não adiciona bibliotecas. A divergência da constituição é preexistente e fora do escopo desta feature. |
| Segurança e autenticação | Atendido | Credencial protegida no backend; DTOs públicos e administrativos expõem somente estado de configuração; operações administrativas e diagnóstico protegidos por `[Authorize]`; nenhum segredo em logs do navegador ou respostas. |
| Persistência e convenções | Atendido | Usar EF Core e PostgreSQL existentes, nome de coluna snake_case, migração EF e associação direta com o agente. |
| Erros e dependências | Atendido | Resultados do diagnóstico sanitizados; chamadas OpenAI existentes permanecem no serviço de infraestrutura e no fluxo atual de injeção. |

**Reavaliação pós-design**: desenho permanece nos projetos e tecnologias existentes, não adiciona dependências nem serviços externos além da OpenAI já usada. A chave usa o protetor AES-GCM existente; configuração de criptografia ausente ou inválida deve resultar em erro seguro ao salvar/ler a credencial, sem fallback para texto puro.

## Project Structure

### Documentation (this feature)

```text
specs/012-openai-agent-key/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── agents-openai.md
└── tasks.md                 # criado em /speckit.tasks
```

### Source Code (repository root)

```text
AvaBot.Domain/Models/Agent.cs
AvaBot.DTO/AgentDTOs.cs
AvaBot.Application/Profiles/AgentProfile.cs
AvaBot.Application/Services/AgentService.cs
AvaBot.Application/Services/ChatService.cs
AvaBot.Application/Services/SearchService.cs
AvaBot.Application/Services/IngestionService.cs
AvaBot.Infra/Context/AvaBotContext.cs
AvaBot.Infra/AppServices/OpenAIService.cs
AvaBot.Infra/Migrations/
AvaBot.API/Controllers/AgentController.cs
AvaBot.Tests/
AvaBot.Tests.API/
frontend/src/types/agent.ts
frontend/src/Services/AgentService.ts
frontend/src/components/admin/AgentForm.tsx
frontend/src/pages/admin/AgentFormPage.tsx
```

**Structure Decision**: Estender o fluxo atual de agente nos projetos existentes. O formulário administrativo mantém o campo e o diagnóstico juntos na seção Modelo de IA; a API de diagnóstico integra o controlador autenticado de agentes; o serviço OpenAI passa a resolver credenciais por agente em vez de manter um único cliente global com uma chave de configuração.

## Complexity Tracking

Não há violações novas da constituição que exijam exceção. A divergência entre as versões descritas na constituição e as versões já usadas pelo repositório foi registrada como preexistente; esta feature não altera a stack.
