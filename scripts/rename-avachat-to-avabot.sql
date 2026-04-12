-- =============================================
-- Rename tables from avachat_ to avabot_
-- PostgreSQL
-- Run this BEFORE deploying the new application version
-- =============================================

BEGIN;

-- 1) Drop existing indexes (old names)
DROP INDEX IF EXISTS avachat_idx_knowledge_files_agent_id;
DROP INDEX IF EXISTS avachat_idx_chat_sessions_agent_id;
DROP INDEX IF EXISTS ix_avachat_chat_sessions_resume_token;
DROP INDEX IF EXISTS avachat_idx_chat_messages_session_id;
DROP INDEX IF EXISTS avachat_idx_telegram_chats_agent_id;
DROP INDEX IF EXISTS avachat_idx_telegram_chats_chat_session_id;

-- 2) Rename tables
ALTER TABLE IF EXISTS avachat_chat_messages    RENAME TO avabot_chat_messages;
ALTER TABLE IF EXISTS avachat_telegram_chats   RENAME TO avabot_telegram_chats;
ALTER TABLE IF EXISTS avachat_chat_sessions    RENAME TO avabot_chat_sessions;
ALTER TABLE IF EXISTS avachat_knowledge_files  RENAME TO avabot_knowledge_files;
ALTER TABLE IF EXISTS avachat_agents           RENAME TO avabot_agents;

-- 3) Rename primary key constraints
ALTER TABLE avabot_agents           RENAME CONSTRAINT avachat_agents_pkey           TO avabot_agents_pkey;
ALTER TABLE avabot_knowledge_files  RENAME CONSTRAINT avachat_knowledge_files_pkey  TO avabot_knowledge_files_pkey;
ALTER TABLE avabot_chat_sessions    RENAME CONSTRAINT avachat_chat_sessions_pkey    TO avabot_chat_sessions_pkey;
ALTER TABLE avabot_chat_messages    RENAME CONSTRAINT avachat_chat_messages_pkey    TO avabot_chat_messages_pkey;
ALTER TABLE avabot_telegram_chats   RENAME CONSTRAINT avachat_telegram_chats_pkey   TO avabot_telegram_chats_pkey;

-- 4) Rename unique constraints
ALTER TABLE avabot_agents RENAME CONSTRAINT avachat_agents_slug_key TO avabot_agents_slug_key;

-- 5) Rename foreign key constraints
ALTER TABLE avabot_knowledge_files RENAME CONSTRAINT avachat_fk_agents_knowledge_files     TO avabot_fk_agents_knowledge_files;
ALTER TABLE avabot_chat_sessions   RENAME CONSTRAINT avachat_fk_agents_chat_sessions       TO avabot_fk_agents_chat_sessions;
ALTER TABLE avabot_chat_messages   RENAME CONSTRAINT avachat_fk_chat_sessions_chat_messages TO avabot_fk_chat_sessions_chat_messages;
ALTER TABLE avabot_telegram_chats  RENAME CONSTRAINT avachat_fk_telegram_chat_agent         TO avabot_fk_telegram_chat_agent;
ALTER TABLE avabot_telegram_chats  RENAME CONSTRAINT avachat_fk_telegram_chat_session       TO avabot_fk_telegram_chat_session;

-- 6) Recreate indexes with new names
CREATE INDEX avabot_idx_knowledge_files_agent_id         ON avabot_knowledge_files (agent_id);
CREATE INDEX avabot_idx_chat_sessions_agent_id           ON avabot_chat_sessions (agent_id);
CREATE UNIQUE INDEX ix_avabot_chat_sessions_resume_token ON avabot_chat_sessions (resume_token);
CREATE INDEX avabot_idx_chat_messages_session_id         ON avabot_chat_messages (chat_session_id);
CREATE INDEX avabot_idx_telegram_chats_agent_id          ON avabot_telegram_chats (agent_id);
CREATE INDEX avabot_idx_telegram_chats_chat_session_id   ON avabot_telegram_chats (chat_session_id);

COMMIT;
