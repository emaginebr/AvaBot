-- Altera foreign keys para CASCADE DELETE
-- Ao deletar um agente, sessoes, mensagens e arquivos sao removidos automaticamente
-- Descobre os nomes reais das constraints para evitar erros

BEGIN;

-- ChatMessages -> ChatSessions
DO $$
DECLARE fk_name TEXT;
BEGIN
    SELECT tc.constraint_name INTO fk_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
    WHERE tc.table_name = 'avachat_chat_messages'
      AND tc.constraint_type = 'FOREIGN KEY'
      AND kcu.column_name = 'chat_session_id';

    IF fk_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE avachat_chat_messages DROP CONSTRAINT %I', fk_name);
    END IF;
END $$;

ALTER TABLE avachat_chat_messages
    ADD CONSTRAINT avachat_fk_chat_sessions_chat_messages
        FOREIGN KEY (chat_session_id)
        REFERENCES avachat_chat_sessions (chat_session_id)
        ON DELETE CASCADE;

-- ChatSessions -> Agents
DO $$
DECLARE fk_name TEXT;
BEGIN
    SELECT tc.constraint_name INTO fk_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
    WHERE tc.table_name = 'avachat_chat_sessions'
      AND tc.constraint_type = 'FOREIGN KEY'
      AND kcu.column_name = 'agent_id';

    IF fk_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE avachat_chat_sessions DROP CONSTRAINT %I', fk_name);
    END IF;
END $$;

ALTER TABLE avachat_chat_sessions
    ADD CONSTRAINT avachat_fk_agents_chat_sessions
        FOREIGN KEY (agent_id)
        REFERENCES avachat_agents (agent_id)
        ON DELETE CASCADE;

-- KnowledgeFiles -> Agents
DO $$
DECLARE fk_name TEXT;
BEGIN
    SELECT tc.constraint_name INTO fk_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name
    WHERE tc.table_name = 'avachat_knowledge_files'
      AND tc.constraint_type = 'FOREIGN KEY'
      AND kcu.column_name = 'agent_id';

    IF fk_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE avachat_knowledge_files DROP CONSTRAINT %I', fk_name);
    END IF;
END $$;

ALTER TABLE avachat_knowledge_files
    ADD CONSTRAINT avachat_fk_agents_knowledge_files
        FOREIGN KEY (agent_id)
        REFERENCES avachat_agents (agent_id)
        ON DELETE CASCADE;

COMMIT;
