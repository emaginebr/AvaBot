-- =============================================
-- Migration: Chave OpenAI por Agente
-- Feature: 012-openai-agent-key
-- Date: 2026-10-08
-- Equivalente manual da migracao EF
--   20261008091218_AddOpenAIApiKeyToAgent
--
-- Adiciona a credencial do agente, cifrada em repouso. O valor em claro nunca
-- entra nesta coluna nem em nenhum DTO: leitura e escrita passam por
-- ISecretProtector (AES-256-GCM), e NULL significa "sem chave propria".
--
-- Executar com:
--   psql "$DATABASE_URL" -f scripts/add-openai-api-key-to-agent.sql
-- ou, no container do banco (ajuste usuario e banco conforme o .env):
--   docker compose exec -T postgres psql -U postgres -d avabot < scripts/add-openai-api-key-to-agent.sql
--
-- Escolha UM caminho: este script OU `dotnet ef database update`. Rodando o
-- script a mao, a linha '20261008091218_AddOpenAIApiKeyToAgent' nao entra em
-- __EFMigrationsHistory, e um `database update` depois vai tentar recriar a
-- coluna e falhar (o DDL gerado pelo EF nao usa IF NOT EXISTS).
-- =============================================

BEGIN;

-- 1) Coluna unica da credencial. Varchar(1000) comporta o payload
--    base64(nonce|tag|ciphertext) de uma API key de ate ~700 caracteres.
ALTER TABLE avabot_agents
    ADD COLUMN IF NOT EXISTS openai_api_key_encrypted VARCHAR(1000);

COMMENT ON COLUMN avabot_agents.openai_api_key_encrypted IS
    'API key OpenAI do agente, cifrada com AES-256-GCM. NULL = agente sem chave propria (sem fallback para a chave global).';

COMMIT;

-- =============================================
-- Reverte com:
--   ALTER TABLE avabot_agents DROP COLUMN IF EXISTS openai_api_key_encrypted;
-- =============================================
