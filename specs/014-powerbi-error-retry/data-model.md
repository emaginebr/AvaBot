# Data Model: diagnóstico completo e retentativas Power BI

**Feature**: `014-powerbi-error-retry` | **Date**: 2026-10-08

## Alteração: `PowerBIQueryLog` (`avabot_powerbi_query_logs`)

Nenhuma entidade nova. Cada requisição ao Power BI que falhar continua sendo armazenada como um registro individual e fica associada à pergunta, dataset e DAX já registrados.

| Propriedade | Coluna | Tipo atual | Tipo após a feature | Regras |
|---|---|---|---|---|
| `ErrorMessage` | `error_message` | `varchar(2000) NULL` | `text NULL` | Diagnóstico integral, incluindo o corpo original da resposta quando houver; sem truncamento fixo; segredo conhecido permanece redigido. |

Os campos já existentes `UserQuestion`, `Query`, `Status`, `DurationMs` e `CreatedAt` descrevem a tentativa individual. Cada repetição automática transitória grava um registro próprio; cada DAX corrigida chamada pelo modelo também grava seu próprio registro. A ordenação existente por `CreatedAt DESC` mantém as tentativas recentes visíveis primeiro.

### Migração e compatibilidade

- Alterar o tipo de `error_message` para PostgreSQL `text`, preservando os valores existentes.
- Remover a restrição `HasMaxLength(2000)` do mapeamento EF Core e a truncagem correspondente antes de persistir, responder ao modelo ou montar a prévia do teste de agente.
- O campo JSON/API `errorMessage` permanece string nullable. Não há alteração no formato do endpoint de histórico.
- O componente do histórico já apresenta a string completa; nenhum campo adicional é requerido para o conteúdo integral.

## Configuração

| Chave | Padrão | Validação | Finalidade |
|---|---:|---|---|
| `PowerBI:MaxQueryAttempts` | `5` | Valor inteiro maior que zero; ausente/inválido usa 5 | Máximo de execuções `consultar_bi` por mensagem, contando execução inicial, correções de DAX e reexecuções temporárias automáticas. |

O limite vive no estado do toolset criado para uma mensagem. Chamadas `listar_schema` não incrementam o contador. A proteção independente `PowerBI:MaxToolCallsPerMessage` permanece ativa para limitar chamadas gerais ao modelo.

## Fluxo de estado da tentativa

```text
Consulta chamada pelo modelo
  ├─ erro DAX corrigível → grava erro → devolve diagnóstico ao modelo
  │    ├─ há orçamento → modelo pode enviar DAX corrigida
  │    └─ sem orçamento → bloqueia execução e modelo finaliza com último diagnóstico
  ├─ 429 / 5xx / timeout / rede → grava erro → espera conforme Retry-After/backoff
  │    ├─ há orçamento → repete a mesma DAX automaticamente
  │    └─ sem orçamento → devolve último diagnóstico ao modelo sem nova requisição
  ├─ 401 / 403 ou erro permanente de permissão → grava diagnóstico sem retry
  └─ sucesso → grava resultado e encerra tentativas
```
