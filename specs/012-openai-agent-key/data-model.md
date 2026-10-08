# Data Model: Chave OpenAI por Agente

## Agent

Representa o agente e a configuração usada por suas operações de IA.

| Campo | Forma persistida / exposta | Regras |
|---|---|---|
| `AgentId` | Identificador numérico existente | Identifica o agente proprietário da credencial. |
| `OpenAIApiKeyEncrypted` | Coluna nullable `openai_api_key_encrypted`, varchar com limite suficiente para o conteúdo cifrado | Único valor persistido da credencial; gravado e lido somente por meio de `ISecretProtector`. `null` significa não configurada. Nunca deve ser mapeado para DTO. |
| `HasOpenAIApiKey` | Booleano derivado nos DTOs de leitura | Indica se existe credencial, sem revelar hint, valor, comprimento ou conteúdo cifrado. Não é persistido como coluna. |

### Relationships

- Cada agente pode ter zero ou uma chave OpenAI.
- Chaves iguais podem pertencer a agentes distintos; não há regra de unicidade.
- A exclusão do agente remove a credencial junto com o registro do agente.

### Validation and lifecycle

1. **Unconfigured**: campo criptografado nulo. Recursos OpenAI do agente não podem ser executados.
2. **Configured**: nova chave é aparada de espaços externos, protegida antes da persistência e usada para operações desse agente.
3. **Unchanged on update**: solicitação sem nova chave e sem intenção de remoção preserva o conteúdo cifrado atual.
4. **Replaced**: nova chave não vazia substitui o segredo cifrado existente.
5. **Removed**: ação explícita limpa o campo persistido; operações subsequentes não podem reutilizar a chave anterior.

O valor em claro existe somente durante o processamento da requisição de configuração/diagnóstico ou no momento necessário para autenticar uma chamada ao provedor. Não registrar o valor em logs.

## OpenAI Connection Diagnostic

Resultado transitório, não persistido.

| Campo | Regras |
|---|---|
| `Success` | `true` se a solicitação de autenticação for aceita; caso contrário, `false`. |
| `Message` | Mensagem segura e acionável. Não contém chave, bearer token nem corpo bruto do provedor. |

O diagnóstico não gera texto, não atualiza a credencial persistida e não guarda a chave submetida. Se a requisição trouxer uma chave ainda não salva, testa essa chave; caso contrário, pode testar a credencial persistida do agente.
