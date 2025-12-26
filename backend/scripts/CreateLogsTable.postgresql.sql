-- Script para criar a tabela de logs do Serilog (PostgreSQL)
-- Execute este script no banco de dados antes de iniciar a aplicação

CREATE TABLE IF NOT EXISTS "Logs" (
    "Id" BIGSERIAL NOT NULL,
    "Message" TEXT NULL,
    "MessageTemplate" TEXT NULL,
    "Level" VARCHAR(128) NULL,
    "TimeStamp" TIMESTAMP NOT NULL,
    "Exception" TEXT NULL,
    "Properties" JSONB NULL,
    "LogEvent" TEXT NULL,
    "SourceContext" VARCHAR(512) NULL,  -- Limitado para permitir índice
    "RequestPath" VARCHAR(512) NULL,     -- Limitado para permitir índice
    "RequestMethod" VARCHAR(10) NULL,    -- GET, POST, PUT, DELETE, etc.
    "StatusCode" INTEGER NULL,
    "Elapsed" DOUBLE PRECISION NULL,
    "UserName" VARCHAR(256) NULL,        -- Limitado para permitir índice (opcional)
    "MachineName" VARCHAR(256) NULL,
    "Environment" VARCHAR(50) NULL,     -- Development, Production, etc.
    CONSTRAINT "PK_Logs" PRIMARY KEY ("Id")
);

-- Índices para melhorar performance de consultas
CREATE INDEX IF NOT EXISTS "IX_Logs_TimeStamp" ON "Logs" ("TimeStamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Logs_Level" ON "Logs" ("Level");
CREATE INDEX IF NOT EXISTS "IX_Logs_SourceContext" ON "Logs" ("SourceContext");
CREATE INDEX IF NOT EXISTS "IX_Logs_RequestPath" ON "Logs" ("RequestPath");

