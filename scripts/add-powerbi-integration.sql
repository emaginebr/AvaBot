-- =============================================
-- Migration: Integracao do Agente com Power BI
-- Feature: 011-powerbi-agent-tools
-- Date: 2026-10-08
-- Equivalente manual da migracao EF
--   20261007213101_AddPowerBIIntegration
--
-- Cria a flag do agente, a credencial de service principal (1:1), os datasets
-- vinculados e o historico de consultas.
--
-- Executar com:
--   psql "$DATABASE_URL" -f scripts/add-powerbi-integration.sql
-- ou, no container do banco (ajuste usuario e banco conforme o .env):
--   docker compose exec -T postgres psql -U postgres -d avabot < scripts/add-powerbi-integration.sql
--
-- Escolha UM caminho: este script OU `dotnet ef database update`. Rodando o
-- script a mao, a linha '20261007213101_AddPowerBIIntegration' nao entra em
-- __EFMigrationsHistory, e um `database update` depois vai tentar recriar as
-- tabelas e falhar (o DDL gerado pelo EF nao usa IF NOT EXISTS).
-- =============================================

BEGIN;

-- 1) Flag que libera as ferramentas de Power BI para o agente.
--    Nasce false para todos os agentes existentes (comportamento atual preservado).
ALTER TABLE avabot_agents
    ADD COLUMN IF NOT EXISTS powerbi_enabled BOOLEAN NOT NULL DEFAULT false;

-- 2) Credenciais do service principal, uma por agente.
--    client_secret_encrypted guarda base64(nonce|tag|ciphertext) do AES-256-GCM;
--    client_secret_hint guarda os 4 ultimos caracteres para a mascara do painel.
CREATE TABLE IF NOT EXISTS avabot_agent_powerbi_configs (
    agent_powerbi_config_id   BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id                  BIGINT                      NOT NULL,
    tenant_id                 VARCHAR(64)                 NOT NULL,
    client_id                 VARCHAR(64)                 NOT NULL,
    client_secret_encrypted   VARCHAR(1000)               NOT NULL,
    client_secret_hint        VARCHAR(8)                  NOT NULL,
    last_test_at              TIMESTAMP WITHOUT TIME ZONE,
    last_test_success         BOOLEAN,
    last_test_message         VARCHAR(2000),
    created_at                TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at                TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avabot_agent_powerbi_configs_pkey PRIMARY KEY (agent_powerbi_config_id),
    -- Sem ON DELETE: AgentRepository.DeleteAsync remove os filhos antes do agente.
    CONSTRAINT avabot_fk_agents_agent_powerbi_configs FOREIGN KEY (agent_id)
        REFERENCES avabot_agents (agent_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS avabot_agent_powerbi_configs_agent_id_key
    ON avabot_agent_powerbi_configs (agent_id);

-- 3) Datasets vinculados ao agente, com o schema gerado.
--    tool_key e o slug unico usado como valor do enum nas ferramentas do modelo.
--    schema_status: 0 NotGenerated, 1 Generated, 2 Partial, 3 Error.
CREATE TABLE IF NOT EXISTS avabot_powerbi_datasets (
    powerbi_dataset_id    BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id              BIGINT                      NOT NULL,
    workspace_id          VARCHAR(64)                 NOT NULL,
    dataset_id            VARCHAR(64)                 NOT NULL,
    name                  VARCHAR(120)                NOT NULL,
    description           VARCHAR(1000),
    tool_key              VARCHAR(60)                 NOT NULL,
    schema_json           JSONB,
    schema_status         INTEGER                     NOT NULL DEFAULT 0,
    schema_generated_at   TIMESTAMP WITHOUT TIME ZONE,
    schema_error          VARCHAR(2000),
    created_at            TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at            TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avabot_powerbi_datasets_pkey PRIMARY KEY (powerbi_dataset_id),
    CONSTRAINT avabot_fk_agents_powerbi_datasets FOREIGN KEY (agent_id)
        REFERENCES avabot_agents (agent_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS avabot_powerbi_datasets_agent_id_dataset_id_key
    ON avabot_powerbi_datasets (agent_id, dataset_id);

CREATE UNIQUE INDEX IF NOT EXISTS avabot_powerbi_datasets_agent_id_tool_key_key
    ON avabot_powerbi_datasets (agent_id, tool_key);

-- 4) Historico de chamadas de ferramenta (listar_schema e consultar_bi).
--    Retencao de N dias por PowerBIQueryLogCleanupService (padrao 30).
--    dataset_name e copia do nome para o historico continuar legivel apos a
--    remocao do dataset.
CREATE TABLE IF NOT EXISTS avabot_powerbi_query_logs (
    powerbi_query_log_id  BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id              BIGINT                      NOT NULL,
    chat_session_id       BIGINT,
    powerbi_dataset_id    BIGINT,
    dataset_name          VARCHAR(120),
    tool_name             VARCHAR(60)                 NOT NULL,
    user_question         TEXT,
    query                 TEXT,
    duration_ms           INTEGER                     NOT NULL,
    row_count             INTEGER,
    truncated             BOOLEAN                     NOT NULL DEFAULT false,
    status                INTEGER                     NOT NULL,
    error_message         VARCHAR(2000),
    created_at            TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avabot_powerbi_query_logs_pkey PRIMARY KEY (powerbi_query_log_id),
    CONSTRAINT avabot_fk_agents_powerbi_query_logs FOREIGN KEY (agent_id)
        REFERENCES avabot_agents (agent_id),
    -- NULL quando a consulta veio da pagina de teste do agente (sem sessao).
    CONSTRAINT avabot_fk_chat_sessions_powerbi_query_logs FOREIGN KEY (chat_session_id)
        REFERENCES avabot_chat_sessions (chat_session_id),
    CONSTRAINT avabot_fk_powerbi_datasets_powerbi_query_logs FOREIGN KEY (powerbi_dataset_id)
        REFERENCES avabot_powerbi_datasets (powerbi_dataset_id)
);

-- Indice da listagem do painel (ORDER BY created_at DESC por agente).
CREATE INDEX IF NOT EXISTS ix_avabot_powerbi_query_logs_agent_id_created_at
    ON avabot_powerbi_query_logs (agent_id, created_at DESC);

CREATE INDEX IF NOT EXISTS IX_avabot_powerbi_query_logs_chat_session_id
    ON avabot_powerbi_query_logs (chat_session_id);

CREATE INDEX IF NOT EXISTS IX_avabot_powerbi_query_logs_powerbi_dataset_id
    ON avabot_powerbi_query_logs (powerbi_dataset_id);

COMMIT;

-- =============================================
-- Reverte com:
--   DROP TABLE IF EXISTS avabot_powerbi_query_logs;
--   DROP TABLE IF EXISTS avabot_powerbi_datasets;
--   DROP TABLE IF EXISTS avabot_agent_powerbi_configs;
--   ALTER TABLE avabot_agents DROP COLUMN IF EXISTS powerbi_enabled;
-- =============================================
