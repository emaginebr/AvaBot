# Contract: ferramentas MCP de controle de acesso do WhatsApp

**Classe**: `AvaBot.API/Mcp/Tools/WhatsappAccessTools.cs` (nova, `[McpServerToolType]`), registrada como as demais classes de ferramentas da feature 017.
**Padrão**: cada ferramenta tem um método público `Xxx(RequestContext<CallToolRequestParams> context, ...)` que delega a `XxxAsync(McpCaller caller, ...)` (testável), e esse método roda dentro de `McpToolResults.RunAsync(_logger, "<nome>", ...)`.
**Agente**: `AgentService.GetOwnedByIdAsync(agentId, caller.UserId) ?? throw new KeyNotFoundException(AgentTools.NotFoundMessage)`.
**Service**: `WhatsappAccessService`, com os mesmos métodos do painel. As mesmas validações (os validators FluentValidation são invocados no service ou na ferramenta, como em `AgentTools`) e as mesmas mensagens de erro, que chegam como `isError`.
**Segredos**: nenhuma entrada ou saída contém `WhatsappToken`. O `McpOutputDtoTest` passa a incluir os DTOs novos.

| # | Nome | Title | Anotações | Entrada | Saída |
|---|---|---|---|---|---|
| 1 | `obter_acesso_whatsapp` | Controle de acesso do WhatsApp | ReadOnly, Idempotent | `agentId` | `WhatsappAccessConfigInfo` |
| 2 | `atualizar_acesso_whatsapp` | Atualizar controle de acesso do WhatsApp | Idempotent | `agentId`, `mode` (1\|2), `offerMessage?`, `confirmationMessage?`, `waitingMessage?`, `declinedMessage?`, `restoreDefaults?` (lista de nomes de frase) | `WhatsappAccessConfigInfo` |
| 3 | `listar_contatos_whatsapp` | Contatos autorizados do WhatsApp | ReadOnly, Idempotent | `agentId` | `List<WhatsappContactInfo>` |
| 4 | `adicionar_contato_whatsapp` | Autorizar contato no WhatsApp | — | `agentId`, `name`, `phone` | `WhatsappContactInfo` |
| 5 | `atualizar_contato_whatsapp` | Editar contato autorizado | Idempotent | `agentId`, `contactId`, `name`, `phone` | `WhatsappContactInfo` |
| 6 | `remover_contato_whatsapp` | Remover contato autorizado | Destructive, Idempotent | `agentId`, `contactId` | `McpMessageInfo` ("Contato removido") |
| 7 | `listar_solicitacoes_whatsapp` | Solicitações de acesso do WhatsApp | ReadOnly, Idempotent | `agentId`, `status?` ("pendente"\|"aprovada"\|"rejeitada") | `List<WhatsappAccessRequestInfo>` |
| 8 | `aprovar_solicitacao_whatsapp` | Aprovar solicitação de acesso | — | `agentId`, `requestId`, `name?` | `WhatsappContactInfo` |
| 9 | `rejeitar_solicitacao_whatsapp` | Rejeitar solicitação de acesso | Destructive | `agentId`, `requestId` | `WhatsappAccessRequestInfo` |

**Semântica de `atualizar_acesso_whatsapp` (parcial)**: diferente do PUT do painel, que envia tudo, aqui um campo de frase omitido ou nulo **mantém** o valor atual. Para voltar ao padrão, a frase é listada em `restoreDefaults` (`"oferta"`, `"confirmacao"`, `"aguardando"`, `"recusa"`). O método monta o `WhatsappAccessConfigUpdateInfo` completo a partir do agente antes de chamar o service.

**`decidedByUserId`**: `caller.UserId`.

**Descriptions** (pt-BR, sem acento, como as ferramentas existentes):
- `obter_acesso_whatsapp`: "Modo de acesso do WhatsApp do agente (1 = todos, 2 = somente contatos autorizados), as quatro frases prontas do fluxo de solicitacao (com o texto padrao) e as quantidades de contatos e de solicitacoes pendentes."
- `aprovar_solicitacao_whatsapp`: "Aprova uma solicitacao de acesso recebida pelo WhatsApp e cria o contato autorizado; a pessoa passa a ser atendida pela IA na proxima mensagem. O nome e opcional (padrao: nome de perfil do WhatsApp)."
- As demais seguem o mesmo estilo.

**`McpServerInstructions`**: acrescentar um parágrafo curto: "Controle de acesso do WhatsApp: no modo 2 so contatos autorizados usam a IA; os demais recebem frases prontas e podem pedir acesso, que o usuario aprova ou rejeita (ferramentas *_whatsapp)."

**Testes**: `AvaBot.Tests/API/Mcp/WhatsappAccessToolsTest.cs` cobre o agente alheio (`isError` com "Agente nao encontrado"), a atualização parcial com `restoreDefaults`, a conversão de `status` textual e o repasse de `caller.UserId` como `decidedByUserId`.
