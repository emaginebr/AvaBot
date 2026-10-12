-- =============================================
-- Baseline do historico de migracoes EF (__EFMigrationsHistory)
-- Date: 2026-10-12
--
-- A partir desta versao a API aplica as migracoes EF pendentes sozinha, na subida
-- (AvaBot.API/Database/StartupMigrator.cs). Os bancos criados antes disso receberam o schema
-- por avabot.sql + scripts/*.sql e nao tem a tabela __EFMigrationsHistory; sem ela o EF
-- tentaria reaplicar todas as migracoes desde o inicio e falharia.
--
-- Este script registra como ja aplicadas as migracoes anteriores a AddUsersAndAgentOwner,
-- conferindo antes, uma a uma, que o objeto que cada migracao cria existe de fato no banco.
-- Se faltar alguma, nada e gravado e a mensagem diz qual script rodar primeiro.
--
-- AddUsersAndAgentOwner (tabela avabot_users) e a primeira migracao que a API aplica
-- sozinha: ela so e registrada aqui se scripts/add-users-and-agent-owner.sql ja tiver sido
-- executado a mao (a tabela existe). Caso contrario a API a aplica na proxima subida.
--
-- Idempotente: pode rodar mais de uma vez.
--
-- Executar com:
--   psql "$DATABASE_URL" -f scripts/seed-ef-migrations-history.sql
-- ou, no container do banco (ajuste usuario e banco conforme o .env):
--   docker exec -i avabot-db psql -U postgres -d avabot < scripts/seed-ef-migrations-history.sql
--
-- Banco novo (vazio): rode avabot.sql, depois este script, depois suba a API.
-- =============================================

BEGIN;

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId"    character varying(150) NOT NULL,
    "ProductVersion" character varying(32)  NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Cada linha: migracao EF, prova de que ela ja esta no banco, script manual equivalente.
CREATE TEMP TABLE ef_baseline ON COMMIT DROP AS
SELECT * FROM (VALUES
    ('20260409132054_CascadeDeleteAgentRelations',
        EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'avabot_fk_chat_sessions_chat_messages' AND confdeltype = 'c'),
        'cascade-delete-agent-relations.sql'),

    ('20260409192042_AddChatModelToAgent',
        EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'avabot_agents' AND column_name = 'chat_model'),
        'add-chat-model-to-agents.sql'),

    ('20260410200641_AddResumeTokenAndTelegramChat',
        EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'avabot_chat_sessions' AND column_name = 'resume_token')
        AND to_regclass('public.avabot_telegram_chats') IS NOT NULL,
        'add-resume-token-and-telegram-chat.sql (ou avabot.sql, que ja inclui)'),

    ('20260412000941_AddTelegramFieldsToAgent',
        EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'avabot_agents' AND column_name = 'telegram_bot_token'),
        'add-telegram-fields-to-agent.sql'),

    ('20260412185748_AddWhatsappTokenToAgent',
        EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'avabot_agents' AND column_name = 'whatsapp_token'),
        'add-whatsapp-token-to-agent.sql'),

    ('20261007213101_AddPowerBIIntegration',
        to_regclass('public.avabot_agent_powerbi_configs') IS NOT NULL
        AND to_regclass('public.avabot_powerbi_datasets') IS NOT NULL
        AND to_regclass('public.avabot_powerbi_query_logs') IS NOT NULL
        AND EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'avabot_agents' AND column_name = 'powerbi_enabled'),
        'add-powerbi-integration.sql'),

    ('20261008091218_AddOpenAIApiKeyToAgent',
        EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'avabot_agents' AND column_name = 'openai_api_key_encrypted'),
        'add-openai-api-key-to-agent.sql'),

    ('20261008214505_ChangePowerBIQueryLogErrorMessageToText',
        EXISTS (SELECT 1 FROM information_schema.columns
                WHERE table_name = 'avabot_powerbi_query_logs' AND column_name = 'error_message' AND data_type = 'text'),
        'powerbi-error-message-to-text.sql')
) AS t (migration_id, applied, script);

DO $$
DECLARE
    product_version CONSTANT text := '9.0.20';
    missing text;
    registered int;
BEGIN
    SELECT string_agg(migration_id || ' (rode scripts/' || script || ')', '; ' ORDER BY migration_id)
      INTO missing
      FROM ef_baseline
     WHERE NOT applied;

    IF missing IS NOT NULL THEN
        RAISE EXCEPTION 'Banco incompleto; aplique antes: %', missing;
    END IF;

    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    SELECT migration_id, product_version FROM ef_baseline
    ON CONFLICT ("MigrationId") DO NOTHING;
    GET DIAGNOSTICS registered = ROW_COUNT;
    RAISE NOTICE '% migracao(oes) anterior(es) registrada(s) agora; as demais ja estavam', registered;

    -- Usuarios (feature 016): registrada so se o script manual ja rodou; senao a API aplica na subida.
    IF to_regclass('public.avabot_users') IS NOT NULL THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20261010201105_AddUsersAndAgentOwner', product_version)
        ON CONFLICT ("MigrationId") DO NOTHING;
        RAISE NOTICE 'avabot_users ja existe: AddUsersAndAgentOwner registrada como aplicada';
    ELSE
        RAISE NOTICE 'avabot_users nao existe: a API aplica AddUsersAndAgentOwner na proxima subida';
    END IF;
END
$$;

COMMIT;

-- Conferir:
--   SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1;
-- Reverter (volta ao estado anterior; a API passa a exigir este script de novo):
--   DROP TABLE IF EXISTS "__EFMigrationsHistory";
