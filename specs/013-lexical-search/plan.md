# Implementation Plan: Busca Textual no Elasticsearch

**Branch**: `013-lexical-search` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-lexical-search/spec.md`

## Summary

Alterar a recuperação da base de conhecimento para enviar texto e agente diretamente ao Elasticsearch, usando correspondência textual obrigatória no conteúdo e mantendo o filtro por agente e o limite de resultados. A etapa de busca deixa de gerar embedding da pergunta ou chamar a OpenAI. A geração de resposta do chat e a geração de embeddings durante a ingestão continuam separadas e inalteradas.

## Technical Context

**Language/Version**: C# / .NET 9 (backend atual)  
**Primary Dependencies**: ASP.NET Core, Elasticsearch .NET Client 8.17.0, OpenAI .NET SDK 2.10.0 (mantido para chat e ingestão)  
**Storage**: Índice existente do Elasticsearch; nenhum novo dado persistido  
**Testing**: Projetos existentes xUnit `AvaBot.Tests` e `AvaBot.Tests.API`; testes automatizados não foram solicitados nesta especificação  
**Target Platform**: API ASP.NET Core existente  
**Project Type**: Serviço web backend em camadas  
**Performance Goals**: Uma busca direta não aguarda geração remota de embedding; preservar o limite de resultados solicitado, com padrão atual de 5  
**Constraints**: Preservar escopo por `agent_id`, forma da resposta e chamadas de busca existentes; não alterar o chat, a ingestão ou os vetores já armazenados  
**Scale/Scope**: Um serviço de busca e seus dois fluxos de entrada: endpoint administrativo direto e recuperação antes de uma resposta de chat

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Evidência / decisão |
|---|---|---|
| Skills obrigatórias | Atendido no planejamento | A implementação backend deve seguir `dotnet-architecture`, conforme a constituição. A skill não está presente no catálogo local atual; disponibilizá-la antes das tarefas de implementação que alteram serviços. |
| Stack e dependências | Atendido com divergência preexistente | O backend atual usa .NET 9; a constituição lista .NET 8. O plano segue a versão existente e não adiciona dependências. A divergência é anterior à feature e não será alterada aqui. |
| Persistência e dados | Atendido | A alteração usa o índice Elasticsearch já existente e não muda esquema, banco relacional nem formato dos documentos. |
| Segurança e escopo | Atendido | O filtro por agente permanece obrigatório. A busca direta preserva autenticação existente no controlador. |
| Dependências externas e erros | Atendido | A recuperação não depende da disponibilidade ou chave da OpenAI; erros do Elasticsearch permanecem erros da operação de busca. A OpenAI continua nos fluxos de geração de resposta e ingestão. |

**Reavaliação pós-design**: o desenho modifica somente interfaces e consulta do serviço de busca. O `MatchQuery` deve ser uma condição obrigatória, além do filtro por agente; deixar o `Should` opcional poderia retornar documentos do agente sem correspondência textual. Não há migração de índice nem novo serviço.

## Project Structure

### Documentation (this feature)

```text
specs/013-lexical-search/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── search-api.md
└── tasks.md                 # criado em /speckit.tasks
```

### Source Code (repository root)

```text
AvaBot.Application/Services/SearchService.cs
AvaBot.Infra.Interfaces/AppServices/IElasticsearchService.cs
AvaBot.Infra/AppServices/ElasticsearchService.cs
AvaBot.Application/Services/ChatService.cs
AvaBot.API/Controllers/AgentController.cs
AvaBot.Application/Services/IngestionService.cs   # comportamento preservado
AvaBot.Infra/AppServices/OpenAIService.cs          # usado por chat/ingestão, não pela busca
```

**Structure Decision**: Manter a arquitetura backend existente. `SearchService` orquestra consulta textual no serviço de infraestrutura Elasticsearch; os consumidores existentes continuam recebendo uma lista de trechos. Nenhuma alteração de frontend, rota ou modelo persistido é necessária.

## Complexity Tracking

Não há violações novas da constituição nem componentes adicionais.
