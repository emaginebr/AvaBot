# Calibração de perguntas

`AvaBot.Calibration` é um programa de console que roda uma pergunta, ou um conjunto de conversas, com a configuração de um agente e gera um relatório em markdown com **todo o fluxo** de cada mensagem. Serve para entender por que o agente respondeu mal e o que ajustar: prompt, descrições do schema, base de conhecimento ou regras do Power BI. O relatório foi pensado para ser lido por uma IA.

O programa **não usa a API, o banco nem o Elasticsearch**:
- **Fluxo:** é o mesmo do teste de agente (`ChatService.TestMessageAsync`), com o mesmo prompt de sistema, as mesmas regras do Power BI e o mesmo loop de ferramentas, executado no próprio processo.
- **Conexões externas:** só a **OpenAI** e o **Power BI**.
- **Base de conhecimento:** lida de uma **pasta local**. Os documentos são divididos nos mesmos trechos da ingestão e buscados com o mesmo critério do índice (match textual).
- **Gravação:** nada é gravado, nem sessão, nem histórico de consultas. A única exceção é o arquivo de schema local (veja abaixo).

## Configuração

Copie o exemplo e preencha. O arquivo `appsettings.json` **fica fora do git**:

```powershell
Copy-Item AvaBot.Calibration/appsettings.Example.json AvaBot.Calibration/appsettings.json
```

```json
{
  "OpenAI": { "ApiKey": "sk-...", "ChatModel": "gpt-4.1-mini" },
  "Agent": {
    "Name": "BIIA",
    "Slug": "biia",
    "Folder": "agent_input/biia",
    "SystemPromptFile": "",
    "SystemPrompt": ""
  },
  "KnowledgeBase": { "Folder": "", "Extensions": [".md", ".txt"], "ChunkSize": 2000, "ChunkOverlap": 200 },
  "PowerBI": {
    "Enabled": true,
    "TenantId": "...", "ClientId": "...", "ClientSecret": "...",
    "MaxQueryAttempts": 5, "MaxRows": 100, "QueryTimeoutSeconds": 30, "MaxToolCallsPerMessage": 5,
    "Workspaces": [],
    "Datasets": []
  },
  "Chat": { "MaxHistoryMessages": 20 },
  "Calibration": { "Question": "Qual foi o volume de exportação da tilápia em 2024?", "File": "", "Output": "" }
}
```

| Seção | O que define |
|---|---|
| `OpenAI` | Chave e modelo usados pelo agente |
| `Agent.Folder` | Pasta no formato `agent_input/<agente>`. O prompt vem de `system_prompt.md` e a base de conhecimento de `docs/`. `SystemPromptFile`, `SystemPrompt` e `KnowledgeBase.Folder` sobrescrevem esses padrões |
| `KnowledgeBase` | Pasta, extensões lidas e tamanho dos trechos (o padrão é o mesmo da ingestão) |
| `PowerBI` | Credenciais do *service principal* e limites (os mesmos da API). `Enabled: false` roda só com a base de conhecimento |
| `PowerBI.Workspaces` | Filtro opcional, por nome ou ID, dos workspaces cujos datasets são oferecidos |
| `PowerBI.Datasets` | Opcional (veja abaixo) |

Caminhos relativos são resolvidos a partir do diretório atual e, se não existirem lá, a partir da raiz do repositório.

### Datasets

Os datasets são **descobertos no Power BI**, pela mesma listagem que o painel usa ao adicionar um dataset:

- **`Datasets` vazio:** todos os datasets que o aplicativo enxerga são oferecidos ao modelo, filtrados por `Workspaces` quando ele estiver preenchido.
- **Item só com `Name`:** os IDs são descobertos pelo nome. Se o mesmo nome existir em mais de um workspace, informe `WorkspaceId` ou use `Workspaces`.
- **Item com `WorkspaceId` e `DatasetId`:** é usado direto, sem listar o Power BI.

```json
"Datasets": [
  { "Name": "ABIPESCA - Comércio Internacional", "Description": "Comércio internacional de pescado (Comtrade)" }
]
```

| Campo | Padrão |
|---|---|
| `ToolKey` | Gerada do nome, como no painel (`abipesca_comercio_internacional`) |
| `Description` | Vazia. No painel ela é escrita pelo administrador e aparece no catálogo de datasets do prompt |
| `SchemaFile` | `calibration/schemas/<ToolKey>.json` |

Para ver o que o aplicativo enxerga:

```powershell
dotnet run --project AvaBot.Calibration -- --list-datasets
```
| `Chat.MaxHistoryMessages` | Limite de histórico nas conversas, igual ao do chat real |
| `Calibration` | Pergunta ou arquivo de conversas padrão e caminho do relatório (vazio: diretório temporário) |

### Schema do dataset

- **O arquivo `SchemaFile` existe** (padrão `calibration/schemas/<ToolKey>.json`): o schema é lido dele. O formato é o mesmo salvo pelo painel, então dá para escrever `userDescription` em tabelas, colunas e medidas e ver na hora o efeito no relatório.
- **O arquivo não existe:** o schema é **gerado no Power BI**, com fórmulas, relacionamentos e valores, e gravado nesse caminho para você editar.
- **Com `--refresh-schema`:** o schema é gerado de novo, e as descrições escritas no arquivo são mantidas.

Os arquivos em `calibration/schemas/` ficam fora do git, porque podem conter valores reais do modelo.

### Secrets necessários

Todos ficam em `AvaBot.Calibration/appsettings.json`, que é local e ignorado pelo git:

| Chave | Secret | Para quê |
|---|---|---|
| `OpenAI:ApiKey` | Chave da API OpenAI | Chamadas ao modelo |
| `PowerBI:TenantId` | ID do tenant Entra ID | Token do Power BI |
| `PowerBI:ClientId` | ID do aplicativo (*service principal*) | Token do Power BI |
| `PowerBI:ClientSecret` | Segredo do aplicativo | Token do Power BI |

Os IDs de workspace e dataset não precisam ser informados, porque são descobertos no Power BI. Também não é preciso chave de criptografia, banco nem login na API. As credenciais são as mesmas cadastradas no painel do agente. O aplicativo precisa ter as mesmas permissões descritas em [POWERBI_INTEGRATION.md](POWERBI_INTEGRATION.md): papel Contributor no workspace ou permissão Build no dataset.

## Execução

```powershell
# pergunta padrão da configuração
dotnet run --project AvaBot.Calibration

# uma pergunta
dotnet run --project AvaBot.Calibration -- -q "Qual foi o volume de exportação da tilápia em 2024?"

# conversas, com relatório num arquivo escolhido
dotnet run --project AvaBot.Calibration -- -f calibration/abipesca.example.json -o calibracao.md

# regerar o schema no Power BI
dotnet run --project AvaBot.Calibration -- --refresh-schema
```

| Opção | Uso |
|---|---|
| `-q`, `--question` | Pergunta isolada |
| `-f`, `--file` | Arquivo JSON de conversas |
| `-o`, `--output` | Arquivo `.md` do relatório |
| `-c`, `--config` | Outro appsettings (por exemplo, um por agente) |
| `--refresh-schema` | Regera o schema dos datasets |
| `--list-datasets` | Lista os workspaces e datasets acessíveis e sai (só precisa das credenciais do Power BI) |

- **Saídas:** o relatório sai no **stdout**; o progresso e o caminho do arquivo saem no **stderr**. Então `dotnet run --project AvaBot.Calibration -- -q "..." > relatorio.md` gera só o markdown.
- **Código de saída:** `0` quando ao menos uma mensagem foi executada, mesmo que a resposta seja ruim; `1` quando nenhuma foi executada; `2` em erro de configuração ou de parâmetros.

### Arquivo de conversas

Cada mensagem é enviada com o histórico das anteriores da mesma conversa: as perguntas e as respostas do agente. É assim que se calibra o caso em que o agente pede esclarecimento e o usuário responde.

```json
{
  "conversations": [
    { "name": "tilapia-2024", "messages": ["Qual foi o volume de exportação da tilápia em 2024?"] },
    { "name": "tilapia-esclarecimento", "messages": [
      "Qual foi o volume de exportação da tilápia em 2024?",
      "Considere todos os produtos de tilápia"
    ]}
  ]
}
```

Se uma mensagem falha, as seguintes da mesma conversa ficam como **não enviadas**, e a próxima conversa segue normalmente.

## Como ler o relatório

O cabeçalho mostra:
- o modelo e a origem do prompt;
- a base de conhecimento, com quantos arquivos e trechos tem;
- a origem do schema de cada dataset, ou o erro que impediu de carregá-lo.

Para cada mensagem:

1. **Pergunta e Resposta**.
2. **Resumo**:
   - indicadores: rodadas do modelo; consultas ao BI, com erro e repetidas; se o limite de consultas foi atingido; tokens de entrada e saída, com a rodada mais cara; tempos do modelo, das ferramentas e total;
   - **sinais de atenção**: a resposta pede informação ao usuário; consulta com erro; nenhuma consulta retornou linhas; limite atingido; DAX repetida; BI indisponível; fluxo interrompido.
3. **Base de conhecimento**: o texto pesquisado e cada trecho encontrado.
4. **Power BI**: os datasets oferecidos, o limite e as execuções, incluindo retentativas automáticas.
5. **Histórico enviado**, nas conversas.
6. **Prompt de sistema** completo.
7. **Rodadas do modelo**:
   - **Rodada 1**: todas as mensagens.
   - **Rodadas seguintes**: só as mensagens novas.
   - **Cada chamada**: a DAX (ou os argumentos) e o **resultado exato devolvido ao modelo**. Para `listar_schema`, é o schema completo; para um erro, é o diagnóstico completo.
   - **Rodada final**: o **prompt final completo** e a **resposta da IA**.

No fim vêm a tabela **Consolidado** e as **instruções para a IA** que vai analisar. Nada é truncado. Chaves `sk-...`, headers `Bearer` e JWTs viram `[redacted]`.

## Pedir a análise a uma IA

- Cole o `.md` numa conversa com uma IA.
- No Claude Code: "rode `dotnet run --project AvaBot.Calibration -- -q \"...\"` e analise o relatório".

Um ciclo típico de calibração:
1. Rodar o programa e ler o relatório.
2. Ajustar o `userDescription` no `SchemaFile`, o `system_prompt.md` ou um documento em `docs/`.
3. Rodar de novo.
4. Levar o que funcionou para o painel do agente.
