# Research: Chave OpenAI por Agente

**Branch**: `012-openai-agent-key`  
**Date**: 2026-10-08

## Current Project Findings

- O agente é persistido em `AvaBot.Domain/Models/Agent.cs`, mapeado em `AvaBot.Infra/Context/AvaBotContext.cs`, atualizado por `AgentService` e exposto por DTOs em `AvaBot.DTO/AgentDTOs.cs`.
- `AgentController` já protege as operações administrativas com `[Authorize]`. `AgentInfo` também é usado por rotas públicas de leitura; por isso, não deve receber o segredo em texto puro.
- `OpenAIService` é atualmente singleton e constrói um cliente usando `OpenAI:ApiKey` na inicialização. A interface de serviço não recebe contexto do agente.
- Chamadas de chat normal, streaming e teste passam por `ChatService`. `SearchService` e `IngestionService` também geram embeddings usando o mesmo serviço OpenAI; ambos têm o identificador do agente disponível no fluxo de chamada.
- O formulário `AgentForm` já possui uma seção Modelo de IA. `AgentFormPage` carrega agentes e envia o DTO completo de criação/edição. `AgentService` registra atualmente os objetos enviados em logs, o que deve ser removido ou sanitizado ao incluir uma credencial.
- O projeto não tem modal administrativo compartilhado. O formulário Power BI fornece um padrão de gestão de segredo protegido, embora seu resultado de diagnóstico atual seja inline.
- `ISecretProtector` e `AesGcmSecretProtector` já fornecem proteção autenticada AES-GCM e são usados pela configuração Power BI. Não se deve criar um segundo mecanismo criptográfico para esta credencial.

## Decisions

### D1. Uma credencial por agente, criptografada em repouso

**Decision**: Persistir o valor criptografado associado ao agente, usando `ISecretProtector`; nunca incluir o valor descriptografado em `AgentInfo`, DTOs públicos, logs ou dados de diagnóstico. Expor somente um indicador de configuração para que a interface saiba que há uma chave salva.

**Rationale**: Atende ao isolamento por agente e às regras de segurança da constituição. A infraestrutura de proteção de segredo já existe e evita introduzir outra dependência ou algoritmo.

**Alternatives considered**:
- Persistir a chave em texto puro: rejeitada por expor um segredo de provedor diretamente no banco.
- Retornar a chave ao frontend para repopular o formulário: rejeitada porque respostas de leitura e logs do navegador ampliariam a exposição.
- Manter uma única chave global em configuração: rejeitada porque não atende à configuração independente por agente.

### D2. Campo em branco preserva; remoção é explícita

**Decision**: O formulário não recebe a chave salva. Um campo vazio sem intenção de remoção mantém a credencial existente. Uma ação explícita de remoção informa a intenção ao backend; uma nova chave não vazia substitui a anterior.

**Rationale**: Implementa a clarificação registrada na spec e impede que uma atualização normal apague a chave. O indicador `hasOpenAIApiKey` diferencia ausência de credencial de segredo oculto.

**Alternatives considered**:
- Enviar a chave salva de volta como texto: rejeitada por revelar o segredo.
- Interpretar qualquer campo vazio como remoção: rejeitada pela decisão explícita da sessão de clarificação.

### D3. Resolver credencial por agente em todas as chamadas OpenAI

**Decision**: Fazer chat, streaming, teste, embeddings da busca e ingestão resolverem a chave do agente relacionado. A resolução e descriptografia ficam no backend; clientes OpenAI devem ser criados/reutilizados por credencial sem capturar uma chave global em singleton.

**Rationale**: A integração existente usa uma credencial central para geração e embeddings. Alterar somente chat deixaria busca e ingestão dependentes da antiga chave global, contrariando o requisito de credencial por agente.

**Alternatives considered**:
- Aplicar chave por agente somente às respostas de chat: rejeitada porque a recuperação semântica e o processamento dos arquivos também chamam a OpenAI.
- Usar a chave global como fallback silencioso: rejeitada porque obscurece agente sem configuração e mantém dependência compartilhada.

### D4. Diagnóstico valida autenticação sem gerar texto

**Decision**: Enviar uma requisição de listagem de modelos da OpenAI com a chave indicada; resultado bem-sucedido significa que a chave autenticou. Nenhum completion é iniciado. O endpoint de diagnóstico aceita uma chave ainda não salva ou usa a chave salva quando a requisição não contém substituição.

**Rationale**: Corresponde à resposta de clarificação, evitando custo de geração e mantendo a verificação server-side. A documentação da API descreve `GET /v1/models` como operação autenticada via bearer token.

**Alternatives considered**:
- Gerar uma resposta de teste: rejeitada por poder consumir créditos e por contrariar a clarificação.
- Testar somente conectividade TCP: rejeitada porque não valida autenticação da chave.

**Limitation**: A listagem confirma autenticação da chave, mas não comprova que o modelo de chat escolhido está autorizado ou disponível para a conta. A interface deve descrever o resultado como validação da conexão/autenticação.

### D5. Sem fallback para a chave global antiga

**Decision**: Operações OpenAI usam a credencial configurada para o agente. Agentes existentes sem credencial precisam ser configurados antes de usar recursos OpenAI; o valor de `OpenAI:ApiKey` não deve ser usado como fallback. A configuração global do modelo de embeddings pode continuar definindo qual modelo de embedding é usado.

**Rationale**: Evita comportamento ambíguo e compartilhamento acidental depois da introdução do isolamento por agente. A spec determina que os recursos OpenAI não estão disponíveis ao agente sem sua própria chave.

**Alternatives considered**:
- Usar `OpenAI:ApiKey` quando a chave do agente estiver ausente: rejeitada por contradizer FR-005/FR-010 e ocultar configuração incompleta.
- Migrar automaticamente a chave global a todos os agentes: rejeitada porque credenciais de configuração podem não existir nos ambientes de produção e copiar segredos automaticamente para várias entidades aumenta a exposição.

## References

- Código atual: `AvaBot.Infra/AppServices/OpenAIService.cs`, `AvaBot.Application/Services/ChatService.cs`, `SearchService.cs`, `IngestionService.cs`, `AvaBot.Infra/AppServices/AesGcmSecretProtector.cs` e `frontend/src/components/admin/AgentForm.tsx`.
- [Documentação oficial da API OpenAI: listagem de modelos](https://platform.openai.com/docs/api-reference/models/list), método GET autenticado para listar modelos.
- [Biblioteca oficial OpenAI para .NET](https://github.com/openai/openai-dotnet), SDK atualmente referenciado pelo projeto.
