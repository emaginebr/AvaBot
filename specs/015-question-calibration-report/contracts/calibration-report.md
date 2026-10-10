# Contrato: execução e formato do relatório de calibração

## Execução

### Script (recomendado)

```powershell
# uma pergunta
./scripts/calibrate-questions.ps1 -Agent abipesca -Question "Qual foi o volume de exportação da tilápia em 2024?"

# conjunto de conversas
./scripts/calibrate-questions.ps1 -File ./calibration/abipesca.json [-Output ./relatorio.md]
```

O script define as variáveis abaixo, roda `dotnet test AvaBot.Tests.API --filter "Category=Calibration"` e imprime o arquivo do relatório no console, em markdown puro, sem o texto do runner. O código de saída é o do `dotnet test`.

### Direto pelo dotnet test

| Variável | Obrigatória | Descrição |
|---|---|---|
| `CALIBRATION_AGENT` | sim* | Slug do agente. *Pode vir do campo `agent` do arquivo |
| `CALIBRATION_QUESTION` | uma das duas | Pergunta isolada |
| `CALIBRATION_FILE` | uma das duas | JSON de conversas (data-model §3) |
| `CALIBRATION_OUTPUT` | não | Caminho do `.md`. Padrão: `%TEMP%/avabot-calibration-<yyyyMMdd-HHmmss>.md` |

Sem `CALIBRATION_AGENT` e sem `agent` no arquivo, o teste fica **Skipped** e a suíte padrão não o executa (FR-018). A URL e as credenciais vêm de `AvaBot.Tests.API/appsettings.json` (`ApiSettings`).

**Resultado do teste**:
- **Passa** quando o relatório foi gerado, mesmo que a resposta do agente seja ruim ou que alguma conversa tenha falhado (FR-015, FR-017).
- **Falha** em login inválido, API inacessível, agente inexistente, arquivo de entrada inválido, ou quando nenhuma das perguntas pôde ser enviada.

## Formato do relatório

Estrutura fixa em markdown, em português:

```markdown
# Relatório de calibração — <nome do agente> (`<slug>`)

- Data: 2026-10-08 18:42:10
- API: http://localhost:5000
- Modelo: gpt-4.1-mini
- Conversas: 2 · Mensagens: 3 · Duração total: 41,2 s

## Conversa 1 — tilapia-2024

### Mensagem 1 de 1

**Pergunta**
<bloco>

**Resposta**
<bloco>

#### Resumo
| Indicador | Valor |
|---|---|
| Rodadas do modelo | 4 |
| Consultas ao BI | 3 (1 com erro, 0 repetidas) |
| Limite de consultas atingido | não (3/5) |
| Tokens (entrada / saída) | 18 340 / 412 — rodada mais cara: 3 (6 120 entrada) |
| Duração | 12,4 s (modelo 7,1 s · ferramentas 5,3 s) |

**Sinais de atenção**
- Consulta com erro na rodada 2.
- A resposta final pede informação ao usuário.
  (ou "Nenhum sinal de atenção.")

#### 1. Base de conhecimento
Texto pesquisado: `<pergunta>`
- Trecho 1 <bloco>
- ...  (ou "Nenhum trecho encontrado.")

#### 2. Power BI
Ferramentas disponíveis: sim · Datasets: ABIPESCA - Comércio Internacional · Limite: 5 consultas
(ou "Ferramentas de BI não disponíveis para este agente.")

#### 3. Histórico enviado
(n mensagens; m omitidas pelo limite) ou "Sem histórico."

#### 4. Prompt de sistema
<bloco>

#### 5. Rodadas do modelo
##### Rodada 1 — tool_calls · 912 ms · 4 120 / 18 tokens
**Mensagens enviadas** (completas na rodada 1)
- `system` <bloco>
- `user` <bloco>
**Decisão do modelo**: chamou `listar_schema`
**Chamada `listar_schema`** · 3 ms
Argumentos <bloco json>
Resultado devolvido ao modelo <bloco>   ← o schema completo aparece aqui (FR-010)

##### Rodada 2 — tool_calls · ...
**Mensagens novas desde a rodada anterior**
- `assistant` → chamou `listar_schema`
- `tool` (call_abc) → (resultado já mostrado na rodada 1)
**Decisão do modelo**: chamou `consultar_bi`
**Chamada `consultar_bi`** · ABIPESCA · 579 ms · erro
DAX <bloco dax>
Resultado devolvido ao modelo <bloco json com o diagnóstico completo>

...

##### Rodada 4 — stop · resposta final
**Prompt final enviado ao modelo** (conjunto completo)
<blocos de todas as mensagens>
**Resposta da IA** <bloco>

(se houver) **Erro**: <mensagem> — rodadas concluídas acima.

## Conversa 2 — ...

## Consolidado
| Conversa | Msg | Rodadas | Consultas | Erros | Sinais | Tokens | Tempo | Status |
|---|---|---|---|---|---|---|---|---|

## Instruções para a IA que analisar este relatório
Para cada mensagem, identifique a etapa em que a resposta se degradou (base de conhecimento,
schema, prompt de sistema, DAX gerada, interpretação do resultado ou decisão de perguntar ao
usuário). Cite a rodada e o trecho. Sugira mudanças concretas em: prompt do agente, descrições
do schema no painel, base de conhecimento ou regras do PromptAddendum. Separe o que é erro
de dados (modelo do Power BI) do que é erro de raciocínio do modelo.
```

Regras de formatação:
- Nenhum conteúdo é truncado: prompts, mensagens, DAX, resultados, diagnósticos e respostas (SC-001).
- Cada conteúdo livre vai num bloco cercado, com a cerca maior que a maior sequência de crases do conteúdo (research R8).
- O resultado de uma ferramenta aparece uma vez, na rodada em que foi executado; nas rodadas seguintes ele é referenciado pelo `toolCallId`, exceto no prompt final, que repete tudo.
- Os segredos são redigidos antes da escrita (research R7) e viram `[redacted]`.
- Turno não enviado: `### Mensagem 2 de 2 — não enviada (falha na mensagem 1)`.
