# Contract: IKnowledgeChunkRepository

`AvaBot.Infra.Interfaces.Repository.IKnowledgeChunkRepository` substitui `AvaBot.Infra.Interfaces.AppServices.IElasticsearchService`. O tipo `ChunkData` vai junto, no mesmo namespace.

```csharp
public interface IKnowledgeChunkRepository
{
    Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks);
    Task DeleteChunksByFileIdAsync(long knowledgeFileId);
    Task DeleteChunksByAgentIdAsync(long agentId);
    Task<List<string>> TextSearchAsync(long agentId, string queryText, int topK = 5);
    Task<List<long>> GetFileIdsMissingChunksAsync();
}
```

| Método | Contrato |
|---|---|
| `IndexChunksAsync` | Insere todos os trechos em uma única transação (`SaveChanges`). Não apaga os anteriores; quem chama apaga antes, como já faz a ingestão. Lista vazia não faz nada. |
| `DeleteChunksByFileIdAsync` / `DeleteChunksByAgentIdAsync` | Removem todos os trechos do arquivo ou do agente. Retornam sem erro quando não há nenhum. |
| `TextSearchAsync` | Só trechos do `agentId`, no máximo `topK`, por relevância. Pergunta sem termos úteis retorna `[]`. Falha do banco **lança** `InvalidOperationException("Não foi possível consultar a base de conhecimento.")`, nunca retorna `[]` (FR-006). |
| `GetFileIdsMissingChunksAsync` | IDs dos arquivos `Ready` sem nenhum trecho, em ordem crescente (backfill). |

`CreateIndexAsync` deixa de existir: o schema vem da migração EF.

Implementações:
- `AvaBot.Infra.Repository.KnowledgeChunkRepository` (produção, Scoped);
- `AvaBot.Calibration.Local.LocalKnowledgeBase` (calibração, somente leitura; `GetFileIdsMissingChunksAsync` retorna `[]`).
