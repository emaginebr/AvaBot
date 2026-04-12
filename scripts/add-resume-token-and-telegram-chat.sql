-- =============================================
-- Adiciona resume_token em avachat_chat_sessions
-- e cria tabela avachat_telegram_chats
-- Executar ANTES do script rename-avachat-to-avabot.sql
-- =============================================

BEGIN;

-- 1) Adiciona coluna resume_token na tabela de sessoes
ALTER TABLE avachat_chat_sessions
    ADD COLUMN IF NOT EXISTS resume_token VARCHAR(32);

-- 2) Preenche registros existentes com token aleatorio
UPDATE avachat_chat_sessions
SET resume_token = substr(md5(random()::text), 1, 32)
WHERE resume_token IS NULL;

-- 3) Aplica NOT NULL apos preencher os existentes
ALTER TABLE avachat_chat_sessions
    ALTER COLUMN resume_token SET NOT NULL;

-- 4) Indice unico no resume_token
CREATE UNIQUE INDEX IF NOT EXISTS ix_avachat_chat_sessions_resume_token
    ON avachat_chat_sessions (resume_token);

-- 5) Cria tabela telegram_chats
CREATE TABLE IF NOT EXISTS avachat_telegram_chats (
    telegram_chat_id    BIGINT                      NOT NULL,
    agent_id            BIGINT                      NOT NULL,
    chat_session_id     BIGINT                      NOT NULL,
    telegram_username   VARCHAR(260),
    telegram_first_name VARCHAR(260),
    created_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avachat_telegram_chats_pkey PRIMARY KEY (telegram_chat_id),
    CONSTRAINT avachat_fk_telegram_chat_agent FOREIGN KEY (agent_id)
        REFERENCES avachat_agents (agent_id) ON DELETE CASCADE,
    CONSTRAINT avachat_fk_telegram_chat_session FOREIGN KEY (chat_session_id)
        REFERENCES avachat_chat_sessions (chat_session_id) ON DELETE CASCADE
);

-- 6) Indices para as foreign keys
CREATE INDEX IF NOT EXISTS avachat_idx_telegram_chats_agent_id
    ON avachat_telegram_chats (agent_id);

CREATE INDEX IF NOT EXISTS avachat_idx_telegram_chats_chat_session_id
    ON avachat_telegram_chats (chat_session_id);

COMMIT;
