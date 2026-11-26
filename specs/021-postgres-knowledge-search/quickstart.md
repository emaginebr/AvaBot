# Quickstart: validar a 021

## Local (sem banco no Windows)

```bash
dotnet build AvaBot.sln
dotnet test AvaBot.Tests/AvaBot.Tests.csproj
grep -rniE "elasticsearch|IElasticsearchService" --include=*.cs --include=*.json --include=*.yml --include=*.yaml --include=*.csproj . \
  | grep -v "/obj/\|/bin/\|node_modules\|/specs/"     # vazio (SC-005)
```

## SQL real (Docker no WSL)

1. `docker run -d --name pg021 -e POSTGRES_PASSWORD=x -p 55432:5432 postgres:17-alpine`
2. Aplicar o schema base (`avabot.sql`) e as migrações. Alternativa: `dotnet ef migrations script` da migração `AddKnowledgeChunks` aplicado sobre as tabelas mínimas.
3. Inserir trechos com o repositório e conferir:
   - "Ação" e "acao" encontram o mesmo trecho;
   - pergunta sem correspondência → 0;
   - pergunta só de stopwords → 0;
   - agente B não vê trechos do agente A;
   - `EXPLAIN` usa o índice GIN.

## Produção (depois do merge)

1. A pipeline aplica a migração na subida da API (`StartupMigrator`).
2. Nos logs da API (`kubectl -n avabot logs deploy/avabot-api`), procurar `[Backfill] concluído: total=N, migrados=X, falhas=Y`.
3. No painel → Arquivos: nenhum arquivo em erro, ou os que estiverem em erro podem ser reprocessados.
4. No painel → Busca na base: termos conhecidos de cada agente retornam trechos.
5. Limpeza do Elasticsearch que ficou no cluster (admin, com o ok do usuário):

   ```bash
   kubectl -n avabot delete statefulset avabot-elasticsearch
   kubectl -n avabot delete service avabot-elasticsearch
   kubectl -n avabot delete pvc data-avabot-elasticsearch-0
   kubectl get pv | grep avabot-elasticsearch   # retain: apagar o PV e o volume na DigitalOcean
   ```
