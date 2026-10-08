-- =============================================
-- Migration: Historico Power BI com erro integral
-- Feature: 014-powerbi-error-retry
-- Date: 2026-10-08
-- Equivalente manual da migracao EF
--   20261008214505_ChangePowerBIQueryLogErrorMessageToText
--
-- Amplia avabot_powerbi_query_logs.error_message de varchar(2000) para text para
-- que o diagnostico integral do Power BI (mensagem principal, detalhes aninhados e
-- corpo original da resposta) caiba por inteiro no historico.
--
-- Executar com:
--   psql "$DATABASE_URL" -f scripts/powerbi-error-message-to-text.sql
-- ou, no container do banco (ajuste usuario e banco conforme o .env):
--   docker compose exec -T postgres psql -U postgres -d avabot < scripts/powerbi-error-message-to-text.sql
--
-- Escolha UM caminho: este script OU `dotnet ef database update`. Rodando o script
-- a mao, a linha '20261008214505_ChangePowerBIQueryLogErrorMessageToText' nao entra
-- em __EFMigrationsHistory, e um `database update` depois vai tentar reaplicar a
-- alteracao e falhar.
--
-- Pre-requisito: a migracao da feature 011 (add-powerbi-integration.sql) tem de ter
-- rodado antes, senao a tabela nao existe.
-- =============================================

BEGIN;

-- 1) Amplia a coluna. USING::text e imediato: varchar e text sao o mesmo tipo
--    interno no PostgreSQL, entao nao ha reescrita de tabela nem perda de valores.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'avabot_powerbi_query_logs'
          AND column_name = 'error_message'
          AND data_type <> 'text'
    ) THEN
        ALTER TABLE avabot_powerbi_query_logs
            ALTER COLUMN error_message TYPE text;
    END IF;
END
$$;

COMMENT ON COLUMN avabot_powerbi_query_logs.error_message IS
    'Diagnostico integral da falha (mensagem principal, detalhes e corpo original da resposta), com segredos redigidos. Sem limite de tamanho.';

COMMIT;

-- =============================================
-- Reverte com:
--   ALTER TABLE avabot_powerbi_query_logs ALTER COLUMN error_message TYPE varchar(2000);
--
-- ATENCAO: diferente do EF, isso falha no PostgreSQL se qualquer registro gravado
-- depois deste upgrade tiver mensagem com mais de 2000 caracteres. O downgrade nao
-- recupera o que o truncate anterior ja havia descartado.
-- =============================================
