# Implementation Plan: Diagnóstico completo e retentativas Power BI

**Branch**: `014-powerbi-error-retry` | **Date**: 2026-10-08 | **Spec**: [spec.md](./spec.md)  
**Input**: Feature specification from `/specs/014-powerbi-error-retry/spec.md`

## Summary

Preservar o diagnóstico integral das respostas de erro do Power BI, encaminhá-lo ao modelo quando uma DAX falhar, habilitar correções sucessivas de consulta e repetir automaticamente falhas transitórias com a mesma DAX. Um orçamento independente limita a cinco execuções BI por mensagem por padrão. As falhas serão armazenadas sem truncamento no Histórico do Power BI.

## Technical Context

**Language/Version**: C# / .NET 9.0 no backend; TypeScript 6 / React 19 na UI administrativa existente  
**Primary Dependencies**: ASP.NET Core, OpenAI .NET SDK 2.10.0, `IHttpClientFactory`, Entity Framework Core 9, Npgsql  
**Storage**: PostgreSQL existente; coluna `error_message` da tabela `avabot_powerbi_query_logs` alterada para `text`  
**Testing**: xUnit 2.9.2 e Moq 4.20; suíte `AvaBot.Tests`  
**Target Platform**: Backend ASP.NET Core e SPA administrativa existente  
**Project Type**: Aplicação web com API backend e frontend React  
**Performance Goals**: Máximo configurável de 5 requisições ao endpoint de consultas por mensagem por padrão. Cada requisição mantém o timeout BI existente; retries temporários aguardam `Retry-After` ou backoff exponencial com jitter e teto de 30 segundos. Não há SLA menor definido para a duração total da conversa.  
**Constraints**: Sem novas dependências nem entidade de negócio; preservar histórico já gravado; não persistir nem enviar segredos; falhas de autenticação/permissão não são retentadas.  
**Scale/Scope**: Uma conversa pode executar até o limite configurado de consultas BI entre correções pelo modelo e retries automáticos. Retenção do histórico continua no padrão existente de 30 dias.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Antes da pesquisa

- **PASS — Stack**: usa os projetos, runtime .NET 9, EF Core/PostgreSQL e SDKs existentes; não adiciona dependências.
- **PASS — Arquitetura**: mudanças ficam em `Infra` (cliente Power BI e mapeamento), `Application` (orquestração e limites), `API`/DTO existente (config e histórico) e testes atuais.
- **PASS — Persistência**: migração EF altera somente o tipo da coluna existente para `text`, sem criar tabela ou entidade; convenção PostgreSQL é mantida.
- **PASS — Segurança e erros**: segredos seguem redigidos, cancelamento do usuário não inicia retry e erros continuam usando o fluxo de tool result, sem exceção vazando para o chat.
- **PASS — Complexidade**: a repetição automática reutiliza cliente e histórico existentes; nenhuma nova biblioteca ou serviço externo é necessário.

### Depois do desenho

- **PASS**: a arquitetura proposta preserva todas as decisões acima. A migração e alteração dos services seguem os padrões EF Core, DI e logging já usados no projeto; nenhuma violação constitucional precisa de exceção.

## Design

### 1. Resposta e exceção do cliente Power BI

Em `AvaBot.Infra/AppServices/PowerBIClient.cs`:

- Ler e reter o corpo completo antes de tratar respostas HTTP não exitosas.
- Expandir `PowerBIApiException` para transportar status, código, diagnóstico completo, corpo original e `Retry-After`, quando disponível.
- Extrair a mensagem principal e todos os textos em `details` aninhados sem substituir um detalhe pela mensagem principal. Se o JSON for inválido ou a estrutura não for conhecida, usar o corpo integral como diagnóstico.
- Inspecionar erros explícitos nos níveis `error`, `results[].error` e `results[].tables[].error`, inclusive em status HTTP 200, antes de aceitar resultado vazio como sucesso.
- Redigir credencial, access token ou outro segredo conhecido no corpo antes de disponibilizá-lo a camadas superiores.
- Não gravar o corpo de erro nos logs operacionais da aplicação; persistência detalhada ocorre no histórico Power BI. Log operacional mantém status e código para evitar exposição de texto sensível.

### 2. Orçamento e fluxo de retries

Em `AvaBot.Application/Services/PowerBIToolProvider.cs` e no encadeamento do chat:

- Adicionar `PowerBI:MaxQueryAttempts`, padrão 5. Valor ausente, inválido ou menor que 1 usa 5.
- Manter um contador por toolset/mensagem das requisições efetivamente enviadas a `consultar_bi`. Inclui a execução inicial, cada retry temporário HTTP e cada nova DAX enviada pelo modelo; não inclui `listar_schema` nem chamadas bloqueadas localmente pelo limite.
- Conferir e reservar orçamento antes de cada requisição ao Power BI, incluindo retries automáticos. Ao esgotar, não enviar outra requisição.
- **DAX/erro de consulta**: registrar a falha e devolver diagnóstico completo estruturado no `ToolChatMessage`; se houver orçamento, instruir o modelo a corrigir a consulta com base no erro. Corrigir a instrução atual que manda parar em qualquer erro. O modelo não deve inventar correções fora do diagnóstico/schema.
- **429**: repetir a mesma DAX após `Retry-After` quando disponível.
- **5xx, timeout por tentativa e falha de rede**: repetir a mesma DAX após backoff exponencial com jitter, começando em aproximadamente 1 s, dobrando por tentativa e limitando o atraso calculado a 30 s.
- Os retries temporários são internos ao executor: o modelo não é chamado para mudar DAX durante eles. Se todos falharem, a falha final detalhada é devolvida para o modelo formular a resposta final.
- **401/403 e falhas de autenticação/permissão**: registrar e devolver sem retry automático nem solicitação para corrigir DAX.
- Gravar um histórico individual por cada requisição HTTP que falhar, incluindo cada replay transitório; gravar cada DAX corrigida como outro registro, com a pergunta original e sua respectiva consulta.
- Preservar a interrupção de cancelamento do usuário sem tratá-la como erro transitório.
- Coordenar `MaxToolCallsPerMessage` com o limite BI, para que a chamada `listar_schema` e o orçamento configurado de consultas não sejam cortados prematuramente pelo limite geral. O orçamento BI continua sendo aplicado no executor e é a barreira efetiva de requests.

### 3. Histórico sem truncamento

- Remover truncamento de 2.000 caracteres no diagnóstico retornado ao modelo e persistido pelo `PowerBIToolset`.
- Remover `HasMaxLength(2000)` de `PowerBIQueryLog.ErrorMessage` no mapeamento do `AvaBotContext` e adicionar migração que converte `error_message` para `text`, sem apagar valores existentes.
- Persistir um diagnóstico serializado que inclua status, código, mensagem integral, detalhes e corpo original integral quando houver. Para falhas de rede sem resposta Power BI, persistir a categoria e mensagem da exceção.
- Manter `PowerBIQueryLogInfo.errorMessage` como string e manter o endpoint paginado. A interface de histórico existente já renderiza o campo integral em texto com quebras de linha; verificar apresentação de mensagens longas.

### 4. Contrato de erro e contexto do modelo

O formato interno e os comportamentos estão em [contracts/powerbi-query-error.md](./contracts/powerbi-query-error.md). O resultado de falha traz categoria, status/código quando disponíveis, mensagem integral, corpo quando recebido, tentativa atual, limite e se ainda cabe correção de DAX. Resultado de sucesso não muda.

## Constitution Check — pós-design

- **PASS**: não há nova entidade, dependência ou projeto; mudança de coluna usa migração EF existente.
- **PASS**: limite configurável e parser pertencem às camadas atuais; o contrato HTTP de histórico permanece compatível (`errorMessage` string).
- **PASS**: cada erro do histórico mantém sanitização de segredos e o corpo completo não é replicado nos logs comuns.
- **PASS**: orçamento de tentativa mantém teto, timeout por chamada, cancelamento e redaction.

## Project Structure

### Documentation (this feature)

```text
specs/014-powerbi-error-retry/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── powerbi-query-error.md
└── tasks.md              # Phase 2 (/speckit.tasks)
```

### Source Code (repository root)

```text
AvaBot.API/
└── appsettings.json                         # PowerBI:MaxQueryAttempts
AvaBot.Application/
└── Services/
    ├── ChatService.cs                        # orçamento máximo do loop de tools
    └── PowerBIToolProvider.cs                # contador, classificação, retry e logging por tentativa
AvaBot.DTO/
└── PowerBIDTOs.cs                            # manter ErrorMessage string integral
AvaBot.Domain/
└── Models/PowerBIQueryLog.cs                 # entidade atual, sem nova entidade
AvaBot.Infra/
├── AppServices/PowerBIClient.cs              # corpo completo, parsing e metadados HTTP
├── Context/AvaBotContext.cs                  # coluna ErrorMessage text
└── Migrations/                               # alteração varchar(2000) → text
AvaBot.Tests/
├── Infra/AppServices/PowerBIClientTest.cs    # parser e classificação HTTP
└── Application/Services/
    ├── PowerBIToolProviderTest.cs             # budget, retries e histórico
    └── ChatServiceTest.cs                    # limite em schema + consulta
frontend/src/components/admin/powerbi/
└── PowerBIQueryLogTable.tsx                  # renderização de diagnóstico integral
```

**Structure Decision**: Alterar os projetos existentes da solução em camadas .NET e reutilizar a tabela, DTO e componente da tela de histórico. Nenhum projeto novo será criado.

## Riscos e mitigação

| Risco | Mitigação |
|---|---|
| Respostas do Power BI usam envelopes de erro diferentes | Parser tolerante para níveis raiz/resultado/tabela; preserva corpo integral se não conseguir extrair detalhes. |
| Diagnósticos completos aumentam tokens enviados ao modelo | Somente erros de DAX são enviados para correção; falhas transitórias repetem internamente a mesma consulta. Sem truncar a mensagem exigida pela especificação. |
| Retries aumentam latência e carga do serviço | Orçamento de cinco execuções, `Retry-After` para 429, backoff com teto e sem repetição de 401/403. |
| Limite geral de tools impediria até cinco consultas BI após ler schema | Separar contador BI e dimensionar o limite do loop OpenAI; executor ainda bloqueia qualquer chamada após orçamento esgotado. |
| Corpo da resposta pode conter dados sensíveis | Redigir credenciais conhecidas antes de persistir/enviar; não duplicar o corpo completo no log operacional. |
| Mensagem antiga no banco pode ter sido truncada antes da migração | Migração amplia o armazenamento para novos erros, mas não reconstrói detalhes que já foram descartados. |

## Complexity Tracking

Sem violações da constituição a justificar. A migração é necessária porque o requisito de mensagem integral ultrapassa o limite atual de 2.000 caracteres.
