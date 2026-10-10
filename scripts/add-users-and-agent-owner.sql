-- =============================================
-- Migration: Contas de usuario e dono dos agentes
-- Feature: 016-user-accounts-auth
-- Date: 2026-10-10
-- Equivalente manual da migracao EF
--   20261010201105_AddUsersAndAgentOwner
--
-- Cria a tabela avabot_users (contas do painel) e a coluna avabot_agents.owner_user_id
-- (dono do agente). A coluna nasce NULA de proposito: a conta do administrador e criada
-- pela propria API na primeira subida (UserBootstrapService), a partir de
-- Auth:Username/Auth:Password, e todos os agentes sem dono sao atribuidos a ela.
-- Se depois disso restar agente sem dono, a API nao sobe e o log explica o motivo.
--
-- Executar com:
--   psql "$DATABASE_URL" -f scripts/add-users-and-agent-owner.sql
-- ou, no container do banco (ajuste usuario e banco conforme o .env):
--   docker compose exec -T postgres psql -U postgres -d avabot < scripts/add-users-and-agent-owner.sql
--
-- Escolha UM caminho: este script OU `dotnet ef database update`. Rodando o script
-- a mao, a linha '20261010201105_AddUsersAndAgentOwner' nao entra em
-- __EFMigrationsHistory, e um `database update` depois vai tentar reaplicar a
-- alteracao e falhar.
-- =============================================

BEGIN;

-- 1) Tabela de usuarios
CREATE TABLE IF NOT EXISTS avabot_users (
    user_id        BIGINT GENERATED ALWAYS AS IDENTITY,
    name           VARCHAR(260)                NOT NULL,
    email          VARCHAR(260)                NOT NULL,
    password_hash  VARCHAR(500)                NOT NULL,
    status         INTEGER                     NOT NULL DEFAULT 1,
    created_at     TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at     TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    CONSTRAINT avabot_users_pkey PRIMARY KEY (user_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS avabot_users_email_key ON avabot_users (email);

COMMENT ON TABLE avabot_users IS 'Contas do painel (feature 016). E-mail normalizado (minusculo, sem espacos) e senha em PBKDF2-SHA256.';
COMMENT ON COLUMN avabot_users.status IS '1 = ativo, 0 = inativo (inativo nao entra; alterado apenas por suporte).';

-- 2) Dono do agente (nulo so ate o bootstrap da API)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'avabot_agents' AND column_name = 'owner_user_id'
    ) THEN
        ALTER TABLE avabot_agents ADD COLUMN owner_user_id BIGINT NULL;
    END IF;
END
$$;

CREATE INDEX IF NOT EXISTS ix_avabot_agents_owner_user_id ON avabot_agents (owner_user_id);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'avabot_fk_users_agents'
    ) THEN
        ALTER TABLE avabot_agents
            ADD CONSTRAINT avabot_fk_users_agents
            FOREIGN KEY (owner_user_id) REFERENCES avabot_users (user_id);
    END IF;
END
$$;

COMMENT ON COLUMN avabot_agents.owner_user_id IS 'Usuario dono do agente (avabot_users). Preenchido pelo bootstrap da API na primeira subida.';

COMMIT;

-- =============================================
-- Reverte com:
--   ALTER TABLE avabot_agents DROP CONSTRAINT IF EXISTS avabot_fk_users_agents;
--   DROP INDEX IF EXISTS ix_avabot_agents_owner_user_id;
--   ALTER TABLE avabot_agents DROP COLUMN IF EXISTS owner_user_id;
--   DROP TABLE IF EXISTS avabot_users;
-- =============================================
