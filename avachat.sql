-- =============================================
-- Avachat Database Schema
-- PostgreSQL
-- =============================================

-- agents
CREATE TABLE avachat_agents (
    agent_id        BIGINT GENERATED ALWAYS AS IDENTITY,
    name            VARCHAR(260)                NOT NULL,
    slug            VARCHAR(260)                NOT NULL,
    description     TEXT,
    system_prompt   TEXT                        NOT NULL,
    status          INTEGER                     NOT NULL DEFAULT 1,
    collect_name    BOOLEAN                     NOT NULL DEFAULT FALSE,
    collect_email   BOOLEAN                     NOT NULL DEFAULT FALSE,
    collect_phone   BOOLEAN                     NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avachat_agents_pkey     PRIMARY KEY (agent_id),
    CONSTRAINT avachat_agents_slug_key UNIQUE (slug)
);

-- knowledge_files
CREATE TABLE avachat_knowledge_files (
    knowledge_file_id   BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id            BIGINT,
    file_name           VARCHAR(500)                NOT NULL,
    file_content        TEXT                        NOT NULL,
    file_size           BIGINT                      NOT NULL,
    processing_status   INTEGER                     NOT NULL DEFAULT 0,
    error_message       TEXT,
    created_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avachat_knowledge_files_pkey PRIMARY KEY (knowledge_file_id),
    CONSTRAINT avachat_fk_agents_knowledge_files FOREIGN KEY (agent_id) REFERENCES avachat_agents (agent_id)
);

-- chat_sessions
CREATE TABLE avachat_chat_sessions (
    chat_session_id BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id        BIGINT,
    user_name       VARCHAR(260),
    user_email      VARCHAR(260),
    user_phone      VARCHAR(50),
    resume_token    VARCHAR(32)                 NOT NULL,
    started_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    ended_at        TIMESTAMP WITHOUT TIME ZONE,

    CONSTRAINT avachat_chat_sessions_pkey PRIMARY KEY (chat_session_id),
    CONSTRAINT avachat_fk_agents_chat_sessions FOREIGN KEY (agent_id) REFERENCES avachat_agents (agent_id)
);

-- chat_messages
CREATE TABLE avachat_chat_messages (
    chat_message_id BIGINT GENERATED ALWAYS AS IDENTITY,
    chat_session_id BIGINT,
    sender_type     INTEGER                     NOT NULL,
    content         TEXT                        NOT NULL,
    created_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avachat_chat_messages_pkey PRIMARY KEY (chat_message_id),
    CONSTRAINT avachat_fk_chat_sessions_chat_messages FOREIGN KEY (chat_session_id) REFERENCES avachat_chat_sessions (chat_session_id)
);

-- telegram_chats
CREATE TABLE avachat_telegram_chats (
    telegram_chat_id    BIGINT                      NOT NULL,
    agent_id            BIGINT                      NOT NULL,
    chat_session_id     BIGINT                      NOT NULL,
    telegram_username   VARCHAR(260),
    telegram_first_name VARCHAR(260),
    created_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT avachat_telegram_chats_pkey PRIMARY KEY (telegram_chat_id),
    CONSTRAINT avachat_fk_telegram_chat_agent FOREIGN KEY (agent_id) REFERENCES avachat_agents (agent_id),
    CONSTRAINT avachat_fk_telegram_chat_session FOREIGN KEY (chat_session_id) REFERENCES avachat_chat_sessions (chat_session_id)
);

-- Indexes
CREATE INDEX avachat_idx_knowledge_files_agent_id   ON avachat_knowledge_files (agent_id);
CREATE INDEX avachat_idx_chat_sessions_agent_id     ON avachat_chat_sessions (agent_id);
CREATE UNIQUE INDEX ix_avachat_chat_sessions_resume_token ON avachat_chat_sessions (resume_token);
CREATE INDEX avachat_idx_chat_messages_session_id   ON avachat_chat_messages (chat_session_id);
