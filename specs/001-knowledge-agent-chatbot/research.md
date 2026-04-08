# Pesquisa Técnica: Chatbot com Agentes de Conhecimento

**Feature**: `001-knowledge-agent-chatbot`
**Criado**: 2026-04-08
**Status**: Finalizado

---

## Visão Geral

Este documento registra as decisões técnicas tomadas para o sistema de chatbot com agentes de conhecimento baseados em RAG (Retrieval-Augmented Generation). Cada seção cobre uma área de decisão crítica, com a escolha adotada, a justificativa e as alternativas consideradas.

---

## 1. Stack Base: Overrides da Constituição do Projeto

A constituição do projeto define .NET 8, React 18, Bootstrap e Context API como padrão. Este projeto deliberadamente sobrescreve essas escolhas.

### 1.1 .NET 9 em vez de .NET 8

- **Decisão**: .NET 9
- **Justificativa**: O .NET 9 (lançado em novembro de 2024) oferece melhorias de performance relevantes no pipeline HTTP e no garbage collector, além de suporte nativo aprimorado para canais assíncronos (`System.Threading.Channels`) — diretamente útil para o fluxo de streaming de tokens via WebSocket. O projeto é novo e não possui dívida técnica com .NET 8, tornando a adoção do .NET 9 sem custo adicional. O `Elastic.Clients.Elasticsearch` (versão mais recente) e o `OpenAI` SDK oficial também têm suporte de primeira classe no .NET 9.
- **Alternativas consideradas**:
  - **.NET 8 (LTS)**: Mais conservador, com suporte de longo prazo até novembro de 2026. Descartado porque o projeto não tem restrições corporativas que exijam LTS, e os ganhos de performance do .NET 9 são relevantes para workloads de I/O intensivo (WebSocket + Elasticsearch + OpenAI).
  - **.NET 10 (preview)**: Imaturo para produção no momento de escrita.

### 1.2 React 19 em vez de React 18

- **Decisão**: React 19
- **Justificativa**: React 19 (lançado em dezembro de 2024) introduz o hook `use()` para suspense baseado em promessas, melhorias no `Suspense` e otimizações no reconciler. Para este projeto, o benefício direto é o uso de `useTransition` aprimorado para atualizações de estado durante o streaming de tokens, evitando travamentos na UI. Além disso, o `react-markdown` e o `react-dropzone` (dependências planejadas) já oferecem suporte ao React 19.
- **Alternativas consideradas**:
  - **React 18**: Estável e amplamente suportado. Descartado em favor do React 19 pela compatibilidade com as dependências escolhidas e pelos ganhos no modelo de concorrência para o caso de uso de streaming.

### 1.3 TailwindCSS em vez de Bootstrap

- **Decisão**: TailwindCSS v4
- **Justificativa**: O sistema de chat exige uma UI customizada e responsiva sem depender de componentes pré-estilizados. TailwindCSS oferece classes utilitárias que permitem construir o layout de chat (bolhas de mensagem, área de digitação, painel lateral de agentes) com controle total sobre o design visual. O bundle final é menor porque só inclui as classes utilizadas. TailwindCSS também se integra naturalmente com `react-markdown` para estilização de conteúdo renderizado via classe `prose` (plugin `@tailwindcss/typography`).
- **Alternativas consideradas**:
  - **Bootstrap 5**: Componentes prontos acelerariam o scaffold inicial, mas impõem estilos padrão difíceis de sobrescrever para uma UI de chat com identidade visual própria. O bundle inclui CSS não utilizado mesmo com purge configurado.
  - **Shadcn/ui + Radix**: Considerado como camada de componentes acessíveis sobre Tailwind, mas adiciona dependências adicionais sem benefício claro para o escopo deste projeto.

### 1.4 Zustand em vez de Context API

- **Decisão**: Zustand
- **Justificativa**: O estado do chat envolve múltiplas fatias independentes: sessão WebSocket, mensagens em stream (atualizadas token a token), dados do usuário coletados, agente ativo e status de upload de arquivos. A Context API do React causa re-renders em toda a árvore de consumidores sempre que qualquer parte do contexto muda — um problema crítico para o streaming de tokens, onde o estado de mensagens muda dezenas de vezes por segundo. O Zustand usa subscriptions seletivas: cada componente se subscreve apenas ao slice de estado que precisa, eliminando re-renders desnecessários. A API é simples (sem boilerplate de reducers), e o estado pode ser acessado fora de componentes React (útil para o handler de mensagens do WebSocket).
- **Alternativas consideradas**:
  - **Context API + useReducer**: Padrão recomendado pela constituição. Descartado pelo problema de re-renders em cascade durante streaming de tokens.
  - **Redux Toolkit**: Poderoso, mas verboso para o escopo deste projeto. O overhead de configuração (slices, reducers, selectors) não se justifica.
  - **Jotai**: Similar ao Zustand em filosofia atômica. Descartado pela menor adoção e ecossistema comparado ao Zustand.

---

## 2. Elasticsearch: Mapeamento de Índice e Busca Híbrida

### 2.1 Mapeamento do Índice para Busca Vetorial

- **Decisão**: Campo `dense_vector` com dimensão 1536 (compatível com `text-embedding-3-small`), similaridade `cosine`, indexado com `hnsw` (Hierarchical Navigable Small World).

```json
{
  "mappings": {
    "properties": {
      "agent_id": { "type": "keyword" },
      "file_id": { "type": "keyword" },
      "chunk_index": { "type": "integer" },
      "content": { "type": "text", "analyzer": "portuguese" },
      "embedding": {
        "type": "dense_vector",
        "dims": 1536,
        "index": true,
        "similarity": "cosine"
      }
    }
  }
}
```

- **Rationale**: `text-embedding-3-small` produz vetores de 1536 dimensões. A similaridade `cosine` é padrão para embeddings de texto normalizado. O campo `content` usa analyzer `portuguese` para BM25 com stemming e remoção de stopwords adequados ao idioma dos documentos. O campo `agent_id` como `keyword` permite filtrar os chunks pelo agente antes de executar a busca.
- **Alternativas consideradas**:
  - **`dot_product` como similaridade**: Mais eficiente computacionalmente, mas exige vetores unitários normalizados. O `text-embedding-3-small` já retorna vetores normalizados, então seria viável — porém `cosine` é mais tolerante a variações e é o padrão recomendado pelo Elasticsearch para embeddings de linguagem.
  - **`flat` index (força bruta)**: Busca exata sem HNSW. Adequado apenas para coleções pequenas (< 10k chunks). Descartado pela falta de escalabilidade.

### 2.2 Busca Híbrida: kNN + BM25 com RRF

- **Decisão**: Usar `sub_searches` com `knn` e `query` (BM25) combinados via `rrf` (Reciprocal Rank Fusion) nativo do Elasticsearch 8.x.

```json
{
  "retriever": {
    "rrf": {
      "retrievers": [
        {
          "standard": {
            "query": {
              "match": { "content": "<query_text>" }
            }
          }
        },
        {
          "knn": {
            "field": "embedding",
            "query_vector": [/* vetor da query */],
            "num_candidates": 50,
            "k": 10,
            "filter": {
              "term": { "agent_id": "<agent_id>" }
            }
          }
        }
      ],
      "rank_window_size": 20,
      "rank_constant": 60
    }
  },
  "size": 5
}
```

- **Rationale**: A busca híbrida combina a precisão semântica do kNN com a exatidão léxica do BM25. O RRF é superior à combinação linear ponderada porque não requer calibração de pesos e lida bem com distribuições de score heterogêneas entre os dois sistemas. `num_candidates: 50` garante qualidade da busca aproximada sem custo excessivo. O filtro por `agent_id` é aplicado dentro do kNN para evitar que chunks de outros agentes contaminem os resultados. O Elasticsearch 8.9+ suporta a sintaxe `retriever` com `rrf` nativamente, sem necessidade de pipeline externo.
- **Alternativas consideradas**:
  - **Busca apenas vetorial (kNN)**: Mais simples, mas falha em queries com termos específicos (siglas, nomes próprios, códigos) que não têm representação semântica clara nos embeddings.
  - **Busca apenas BM25**: Não captura similaridade semântica para perguntas reformuladas ou sinônimos.
  - **Combinação linear manual**: Requer calibração de pesos (`alpha * knn_score + (1-alpha) * bm25_score`) e normalização dos scores. Mais frágil que RRF.
  - **Pinecone / Weaviate / Qdrant**: Bancos vetoriais dedicados. Descartados para não adicionar outro serviço de infraestrutura ao stack, dado que o Elasticsearch 8.x cobre o caso de uso com desempenho adequado.

---

## 3. Cliente .NET para Elasticsearch: Elastic.Clients.Elasticsearch vs NEST

- **Decisão**: `Elastic.Clients.Elasticsearch` (versão 8.x, cliente oficial de nova geração)
- **Rationale**: O NEST é o cliente legado (v7 e anteriores) e não recebe atualizações ativas para Elasticsearch 8.x. O `Elastic.Clients.Elasticsearch` é o cliente oficial gerado a partir da especificação da API do Elasticsearch, com suporte total ao .NET 9, tipagem forte para a nova sintaxe de `retriever`/`rrf` e suporte nativo a `dense_vector` e operações kNN. O cliente tem fluent API compatível com a sintaxe de busca híbrida descrita na seção anterior e suporta injeção de dependência via `services.AddElasticsearchClient()`.
- **Alternativas consideradas**:
  - **NEST (v7)**: Compatível com Elasticsearch 7.x. Para Elasticsearch 8.x exigiria usar o modo de compatibilidade (`ElasticCompatibilityMode`), perdendo acesso às novas APIs (kNN nativo, RRF, retrievers). Descartado.
  - **Elasticsearch REST API direta via `HttpClient`**: Flexível, mas exige serialização/deserialização manual de JSON e gestão de erros sem tipagem. Descartado pela manutenibilidade.

---

## 4. WebSocket: Middleware Nativo vs SignalR

- **Decisão**: Middleware nativo de WebSocket do ASP.NET Core (`app.UseWebSockets()`)
- **Rationale**: O spec define explicitamente WebSocket nativo. Para este projeto, o protocolo de comunicação é simples e controlado: o cliente envia uma mensagem de texto e o servidor responde com um stream de tokens seguido de um evento de conclusão. Não há necessidade de grupos, reconexão automática, negociação de transporte ou múltiplos protocolos — tudo isso que SignalR oferece. O WebSocket nativo tem menor overhead de protocolo, sem o envelope adicional do SignalR, e é mais fácil de interoperar com qualquer cliente (incluindo clientes não-JavaScript). A implementação segue o padrão:

```csharp
app.UseWebSockets();
app.Map("/ws/chat/{agentSlug}", async (HttpContext ctx, string agentSlug) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    await chatHandler.HandleAsync(ws, agentSlug, ctx.RequestAborted);
});
```

- **Alternativas consideradas**:
  - **SignalR**: Oferece reconexão automática, fallback para long-polling e grupos de conexão. Descartado porque adiciona complexidade desnecessária para o caso de uso (chat 1:1 sem necessidade de broadcast ou reconexão com estado) e o spec especifica WebSocket nativo explicitamente.
  - **gRPC streaming**: Adequado para comunicação servidor-cliente tipada. Descartado pela falta de suporte nativo em browsers sem proxy (gRPC-Web) e pela complexidade adicional.
  - **Server-Sent Events (SSE)**: Unidirecional (servidor → cliente). Descartado porque o chat requer comunicação bidirecional (cliente envia perguntas, servidor envia respostas em stream).

---

## 5. Streaming da OpenAI via WebSocket

- **Decisão**: Usar `OpenAI.Chat.ChatClient` com `CompleteChatStreamingAsync()` do SDK oficial `OpenAI` para .NET, e repassar cada token para o WebSocket como frame de texto JSON com tipo `token`.

**Fluxo**:
1. Cliente envia mensagem via WebSocket (JSON com campo `message`)
2. Backend executa busca híbrida no Elasticsearch e recupera os chunks mais relevantes
3. Backend monta o prompt com system prompt + contexto RAG + histórico + mensagem do usuário
4. Backend chama `CompleteChatStreamingAsync()` e itera sobre os `StreamingChatCompletionUpdate`
5. Para cada token recebido: serializa como `{"type":"token","content":"..."}` e envia via `ws.SendAsync()`
6. Ao finalizar: envia `{"type":"done"}` e persiste a mensagem completa no PostgreSQL

```csharp
await foreach (var update in chatClient.CompleteChatStreamingAsync(messages, cancellationToken: ct))
{
    foreach (var part in update.ContentUpdate)
    {
        var frame = JsonSerializer.SerializeToUtf8Bytes(new { type = "token", content = part.Text });
        await ws.SendAsync(frame, WebSocketMessageType.Text, true, ct);
    }
}
await ws.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { type = "done" }), WebSocketMessageType.Text, true, ct);
```

- **Rationale**: O SDK oficial da OpenAI para .NET (`OpenAI` NuGet package) suporta streaming nativo com `IAsyncEnumerable`, o que se integra naturalmente com o pipeline assíncrono do ASP.NET Core. Repassar tokens individualmente via WebSocket (em vez de bufferizar a resposta completa) é essencial para a experiência de "digitação progressiva" definida no spec. O frame `done` permite ao frontend saber quando parar de mostrar o indicador de digitação e iniciar a persistência local.
- **Alternativas consideradas**:
  - **Bufferizar a resposta completa antes de enviar**: Elimina a percepção de streaming — o usuário esperaria a resposta inteira antes de ver qualquer texto. Descartado.
  - **`HttpClient` direto para a API da OpenAI com `Stream`**: Requer parsing manual do formato SSE (`data: {...}\n\n`) da API da OpenAI. O SDK oficial abstrai isso corretamente. Descartado.
  - **Azure OpenAI**: Alternativa para controle de região e compliance. Não descartado definitivamente, mas o SDK `OpenAI` para .NET suporta ambos com a mesma interface — a troca seria apenas de configuração.

---

## 6. Estratégia de Chunking de Markdown

- **Decisão**: Chunking por caracteres com tamanho alvo de ~2000 caracteres e overlap de ~200 caracteres, com respeito a quebras de parágrafo (splitting em `\n\n` como ponto preferencial).

**Algoritmo**:
1. Fazer parse do Markdown para extrair texto puro (remover sintaxe MD que não agrega semântica: `#`, `**`, `_`, etc.) usando `Markdig`
2. Dividir o texto em blocos por `\n\n` (parágrafos)
3. Acumular blocos até atingir ~2000 caracteres; quando o limite for atingido, fechar o chunk e iniciar o próximo incluindo os últimos ~200 caracteres do chunk anterior como overlap
4. Chunks muito curtos (< 100 caracteres) são agrupados com o próximo

- **Rationale**: 2000 caracteres (~400–500 tokens) é o tamanho ideal para manter contexto semântico suficiente em cada chunk sem ultrapassar o limite de tokens do modelo de embedding (`text-embedding-3-small` suporta até 8191 tokens, mas chunks maiores diluem a especificidade semântica). O overlap de 200 caracteres evita que informações relevantes sejam cortadas na fronteira entre chunks. Respeitar quebras de parágrafo mantém a coerência semântica dentro de cada chunk, evitando divisões no meio de frases. O uso de `Markdig` para parsing garante que a sintaxe Markdown não gere ruído nos embeddings (ex: `# Título` deve ser tratado como "Título", não como "# Título").
- **Alternativas consideradas**:
  - **Chunking por tokens (via tiktoken)**: Mais preciso em relação ao limite do modelo de embedding. Descartado pela complexidade de adicionar um tokenizer .NET (`Microsoft.ML.Tokenizers`) e porque o mapeamento caracteres→tokens (~4 chars/token) é suficientemente estável para `text-embedding-3-small`.
  - **Chunking por headings Markdown (H1/H2/H3)**: Divide o conteúdo por seções semânticas. Adequado para documentos bem estruturados, mas gera chunks de tamanho muito variável (de 50 a 10000+ caracteres) se o documento não for consistente. Descartado pela imprevisibilidade.
  - **LangChain RecursiveCharacterTextSplitter (Python)**: Referência conceitual; adaptado para .NET com a lógica descrita acima. Não há equivalente direto no ecossistema .NET, então a implementação é própria.
  - **Tamanho de 500 caracteres**: Chunks menores têm mais contexto específico, mas menos coerência semântica e geram mais documentos no índice. 2000 caracteres oferece melhor equilíbrio para documentação técnica.

---

## 7. Persistência do Histórico de Chat no PostgreSQL

- **Decisão**: Persistência em tempo real das mensagens no PostgreSQL, com janela de contexto em memória configurável via `appsettings.json`.

**Esquema**:
```sql
-- Sessão de chat (uma por conexão WebSocket)
chat_sessions (id, agent_id, user_name, user_email, user_phone, created_at, ended_at)

-- Mensagens individuais
chat_messages (id, session_id, role [user|assistant], content, created_at)
```

**Fluxo de contexto**:
1. Ao iniciar a sessão, carregar as últimas N mensagens da sessão do PostgreSQL (caso o usuário reconecte)
2. Manter em memória apenas as últimas `ContextWindowSize` mensagens (configurável, padrão: 20)
3. A cada mensagem do usuário: persistir imediatamente no banco antes de chamar a OpenAI
4. A cada mensagem do assistente: persistir o conteúdo completo após o `done` do streaming

**Configuração (`appsettings.json`)**:
```json
{
  "Chat": {
    "ContextWindowSize": 20,
    "MaxTokensPerMessage": 4000
  }
}
```

- **Rationale**: Persistir em tempo real garante que nenhuma mensagem seja perdida em caso de desconexão ou falha do servidor. A janela de contexto em memória evita que o payload enviado à OpenAI cresça indefinidamente — conversas longas podem facilmente ultrapassar o limite de tokens do `gpt-4o` (128k tokens) se todo o histórico for incluído. Separar `chat_sessions` de `chat_messages` permite:
  - Associar os dados coletados do usuário à sessão (não à mensagem)
  - Consultar o histórico de uma sessão específica
  - Calcular métricas por sessão (duração, número de mensagens, agente usado)
- **Alternativas consideradas**:
  - **Histórico apenas em memória (sem persistência)**: Simples, mas perde todo o histórico em caso de restart do servidor ou desconexão. Inaceitável para o requisito de persistência do spec.
  - **Persistir apenas ao final da sessão**: Risco de perda de dados se a conexão cair antes do encerramento formal. Descartado.
  - **Redis para histórico em memória + PostgreSQL para arquivo frio**: Adiciona Redis como dependência. Descartado porque a janela de contexto em memória é gerenciada por processo (uma instância por conexão WebSocket), e não há necessidade de compartilhar estado entre instâncias para o modelo atual.
  - **Usar o PostgreSQL como única fonte de verdade (sem cache em memória)**: Cada mensagem exigiria uma query ao banco para montar o contexto. Descartado pelo latência adicional no caminho crítico do chat.

---

## 8. Fluxo de Coleta de Dados do Usuário

- **Decisão**: Coleta realizada via mensagens do próprio WebSocket antes de habilitar o chat normal, com estado da sessão gerenciado no servidor.

**Fluxo**:
1. Cliente abre conexão WebSocket
2. Servidor verifica quais campos o agente requer (`required_fields: ["name", "email", "phone"]`)
3. Se houver campos obrigatórios, servidor envia `{"type":"collect","field":"name","label":"Qual é o seu nome?"}` 
4. Cliente exibe input específico para o campo e envia `{"type":"field_response","field":"name","value":"João"}`
5. Servidor valida e persiste o valor; repete para os próximos campos obrigatórios
6. Após coletar todos os campos, servidor envia `{"type":"ready"}` e habilita o chat normal
7. Se o agente não requer nenhum campo: servidor envia `{"type":"ready"}` imediatamente após a conexão

**Estado no servidor**:
```csharp
enum CollectionState { CollectingFields, Ready }

class ChatSession
{
    public CollectionState State { get; set; }
    public int NextFieldIndex { get; set; }
    public Dictionary<string, string> CollectedFields { get; set; }
}
```

- **Rationale**: Usar o próprio canal WebSocket para a coleta de dados (em vez de um formulário HTML separado antes do chat) permite uma experiência unificada: o usuário já está na interface de chat e não precisa preencher um formulário externo. O fluxo campo a campo (em vez de um formulário com todos os campos de uma vez) é mais conversacional e alinhado com a UX de chatbot. A validação dos campos ocorre no servidor, que controla a transição de estado — o frontend não pode burlar a coleta enviando uma mensagem de chat antes do `ready`. Os dados coletados são persistidos na `chat_sessions` antes de iniciar o chat, garantindo integridade.
- **Alternativas consideradas**:
  - **Formulário HTML separado antes do WebSocket**: Mais simples de implementar, mas quebra a experiência de usuário — o usuário percebe que está preenchendo um formulário, não conversando com o agente. Descartado.
  - **Coletar dados via HTTP REST antes de abrir o WebSocket**: Requer uma chamada HTTP adicional e gestão de um token de sessão para associar os dados ao WebSocket subsequente. Mais complexo sem benefício claro. Descartado.
  - **Delegar toda a coleta ao frontend (sem validação no servidor)**: O servidor simplesmente aceitaria qualquer mensagem e esperaria que o frontend tivesse coletado os dados. Inseguro e inconsistente. Descartado.
  - **Enviar todos os campos de uma vez como formulário JSON**: `{"type":"collect_form","fields":[...]}`. Mais eficiente em número de mensagens, mas menos conversacional. Viável como otimização futura.

---

## 9. Arquitetura CQRS com MediatR

- **Decisão**: Usar MediatR para separar comandos e queries no domínio de agentes e arquivos. O fluxo de chat via WebSocket usa handlers diretos (sem MediatR no caminho crítico de streaming).
- **Rationale**: MediatR é adequado para operações de CRUD dos agentes e arquivos (criação, edição, listagem, deleção), onde o overhead de despacho via mediator é irrelevante. No caminho crítico do chat (WebSocket → Elasticsearch → OpenAI → WebSocket), o MediatR adicionaria uma camada de indireção sem benefício e dificultaria o controle fino do streaming assíncrono com `IAsyncEnumerable`. O handler de chat é injetado diretamente no middleware de WebSocket.
- **Alternativas consideradas**:
  - **MediatR em todo o pipeline incluindo o chat**: Possível usando `IStreamRequest<T>` do MediatR para streaming. Descartado pela complexidade adicional sem ganho real de testabilidade para o fluxo de streaming.
  - **Sem MediatR (services diretos)**: Viável, mas o MediatR facilita a adição de behaviors transversais (logging, validação, retry) nas operações de CRUD dos agentes. Mantido para esse escopo.

---

## 10. Validação com FluentValidation

- **Decisão**: FluentValidation para todos os DTOs de entrada das APIs REST (criação/edição de agentes, upload de arquivos). Validação inline para dados coletados via WebSocket.
- **Rationale**: FluentValidation integra com o pipeline de validação automática do ASP.NET Core via `AddFluentValidationAutoValidation()`, retornando erros 400 com detalhes estruturados sem código adicional nos controllers. Para o WebSocket, a validação dos campos coletados é mais simples (verificar se não está vazio, formato de email, formato de telefone) e é implementada inline no handler de sessão.
- **Alternativas consideradas**:
  - **Data Annotations**: Mais simples, mas menos expressivo para regras complexas (ex: slug deve ser kebab-case, campos opcionais com regras condicionais). Descartado em favor do FluentValidation para consistência com o padrão do projeto.

---

## Resumo das Decisões

| Área | Decisão |
|---|---|
| Runtime backend | .NET 9 |
| Framework frontend | React 19 |
| Estilização | TailwindCSS v4 |
| Estado frontend | Zustand |
| Banco vetorial | Elasticsearch 8.x (nativo) |
| Cliente Elasticsearch | `Elastic.Clients.Elasticsearch` |
| Busca | kNN + BM25 com RRF |
| Embeddings | `text-embedding-3-small` (1536 dims) |
| Chat LLM | `gpt-4o` via OpenAI SDK para .NET |
| Transporte real-time | WebSocket nativo ASP.NET Core |
| Chunking | ~2000 chars, ~200 overlap, split em `\n\n` |
| Persistência | PostgreSQL (sessões + mensagens em tempo real) |
| Contexto em memória | Janela configurável via `appsettings.json` |
| Coleta de dados | Fluxo via WebSocket, campo a campo |
| CQRS | MediatR (CRUD de agentes/arquivos) |
| Validação | FluentValidation (REST) + inline (WebSocket) |
