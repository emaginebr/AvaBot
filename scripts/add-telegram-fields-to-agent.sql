-- Migration: Add Telegram fields to avabot_agents
-- Feature: 003-telegram-per-agent-config
-- Date: 2026-04-11

ALTER TABLE avabot_agents ADD COLUMN telegram_bot_name varchar(260);
ALTER TABLE avabot_agents ADD COLUMN telegram_bot_token varchar(260);
ALTER TABLE avabot_agents ADD COLUMN telegram_webhook_secret varchar(260);

CREATE UNIQUE INDEX ix_avabot_agents_telegram_bot_token
    ON avabot_agents (telegram_bot_token)
    WHERE telegram_bot_token IS NOT NULL;
