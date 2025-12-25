-- Script para criar a tabela de logs do Serilog
-- Execute este script no banco de dados antes de iniciar a aplicação

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Logs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Logs] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL,
        [Message] NVARCHAR(MAX) NULL,
        [MessageTemplate] NVARCHAR(MAX) NULL,
        [Level] NVARCHAR(128) NULL,
        [TimeStamp] DATETIME2 NOT NULL,
        [Exception] NVARCHAR(MAX) NULL,
        [Properties] NVARCHAR(MAX) NULL,
        [LogEvent] NVARCHAR(MAX) NULL,
        [SourceContext] NVARCHAR(512) NULL,  -- Limitado para permitir índice
        [RequestPath] NVARCHAR(512) NULL,     -- Limitado para permitir índice
        [RequestMethod] NVARCHAR(10) NULL,    -- GET, POST, PUT, DELETE, etc.
        [StatusCode] INT NULL,
        [Elapsed] FLOAT NULL,
        [UserName] NVARCHAR(256) NULL,        -- Limitado para permitir índice (opcional)
        [MachineName] NVARCHAR(256) NULL,
        [Environment] NVARCHAR(50) NULL,     -- Development, Production, etc.
        CONSTRAINT [PK_Logs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    -- Índices para melhorar performance de consultas
    CREATE NONCLUSTERED INDEX [IX_Logs_TimeStamp] ON [dbo].[Logs] ([TimeStamp] DESC);
    CREATE NONCLUSTERED INDEX [IX_Logs_Level] ON [dbo].[Logs] ([Level]);
    CREATE NONCLUSTERED INDEX [IX_Logs_SourceContext] ON [dbo].[Logs] ([SourceContext]);
    CREATE NONCLUSTERED INDEX [IX_Logs_RequestPath] ON [dbo].[Logs] ([RequestPath]);
END
GO

