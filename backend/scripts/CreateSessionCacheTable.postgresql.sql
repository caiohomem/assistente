-- Script para criar a tabela de cache de sessão (PostgreSQL)
-- Execute este script no banco de dados antes de iniciar a aplicação
-- Nota: .NET não tem suporte nativo para PostgreSQL distributed cache
-- Este script é apenas para referência caso você implemente um provider customizado
-- Por padrão, a aplicação usará Memory Cache quando detectar PostgreSQL

-- Tabela de cache de sessão (estrutura similar ao SQL Server)
CREATE TABLE IF NOT EXISTS "SessionCache" (
    "Id" VARCHAR(449) NOT NULL,
    "Value" BYTEA NOT NULL,
    "ExpiresAtTime" BIGINT NOT NULL,
    "SlidingExpirationInSeconds" BIGINT NULL,
    "AbsoluteExpiration" BIGINT NULL,
    CONSTRAINT "PK_SessionCache" PRIMARY KEY ("Id")
);

-- Índice para limpeza de sessões expiradas
CREATE INDEX IF NOT EXISTS "IX_SessionCache_ExpiresAtTime" ON "SessionCache" ("ExpiresAtTime");

