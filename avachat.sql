-- =============================================
-- Avachat Database Schema
-- PostgreSQL
-- =============================================

-- agents
CREATE TABLE agents (
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

    CONSTRAINT agents_pkey     PRIMARY KEY (agent_id),
    CONSTRAINT agents_slug_key UNIQUE (slug)
);

-- knowledge_files
CREATE TABLE knowledge_files (
    knowledge_file_id   BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id            BIGINT,
    file_name           VARCHAR(500)                NOT NULL,
    file_content        TEXT                        NOT NULL,
    file_size           BIGINT                      NOT NULL,
    processing_status   INTEGER                     NOT NULL DEFAULT 0,
    error_message       TEXT,
    created_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at          TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT knowledge_files_pkey PRIMARY KEY (knowledge_file_id),
    CONSTRAINT fk_agents_knowledge_files FOREIGN KEY (agent_id) REFERENCES agents (agent_id)
);

-- chat_sessions
CREATE TABLE chat_sessions (
    chat_session_id BIGINT GENERATED ALWAYS AS IDENTITY,
    agent_id        BIGINT,
    user_name       VARCHAR(260),
    user_email      VARCHAR(260),
    user_phone      VARCHAR(50),
    started_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    ended_at        TIMESTAMP WITHOUT TIME ZONE,

    CONSTRAINT chat_sessions_pkey PRIMARY KEY (chat_session_id),
    CONSTRAINT fk_agents_chat_sessions FOREIGN KEY (agent_id) REFERENCES agents (agent_id)
);

-- chat_messages
CREATE TABLE chat_messages (
    chat_message_id BIGINT GENERATED ALWAYS AS IDENTITY,
    chat_session_id BIGINT,
    sender_type     INTEGER                     NOT NULL,
    content         TEXT                        NOT NULL,
    created_at      TIMESTAMP WITHOUT TIME ZONE NOT NULL,

    CONSTRAINT chat_messages_pkey PRIMARY KEY (chat_message_id),
    CONSTRAINT fk_chat_sessions_chat_messages FOREIGN KEY (chat_session_id) REFERENCES chat_sessions (chat_session_id)
);

-- Indexes
CREATE INDEX idx_knowledge_files_agent_id   ON knowledge_files (agent_id);
CREATE INDEX idx_chat_sessions_agent_id     ON chat_sessions (agent_id);
CREATE INDEX idx_chat_messages_session_id   ON chat_messages (chat_session_id);
