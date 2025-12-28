#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ErrorCode,
  McpError,
} from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from './api-client.js';
import { createContactTools, handleContactTool } from './tools/contacts.js';
import { createReminderTools, handleReminderTool } from './tools/reminders.js';
import { createNoteTools, handleNoteTool } from './tools/notes.js';
import {
  createAutomationTools,
  handleAutomationTool,
} from './tools/automation.js';
import { createCreditTools, handleCreditTool } from './tools/credits.js';
import {
  createAgentConfigTools,
  handleAgentConfigTool,
} from './tools/agent-config.js';
import { createCaptureTools, handleCaptureTool } from './tools/capture.js';
import { createPlanTools, handlePlanTool } from './tools/plans.js';
import {
  createEmailTemplateTools,
  handleEmailTemplateTool,
} from './tools/email-templates.js';

// Configuração do servidor MCP
const API_BASE_URL =
  process.env.API_BASE_URL || 'http://localhost:5239';
const ACCESS_TOKEN = process.env.ACCESS_TOKEN || '';

// Criar cliente da API
const apiClient = new ApiClient({
  baseUrl: API_BASE_URL,
  accessToken: ACCESS_TOKEN,
});

// Se o token foi fornecido via variável de ambiente, configurá-lo
if (ACCESS_TOKEN) {
  apiClient.setAccessToken(ACCESS_TOKEN);
}

// Criar servidor MCP
const server = new Server(
  {
    name: 'assistente-executivo-mcp',
    version: '1.0.0',
  },
  {
    capabilities: {
      tools: {},
    },
  }
);

// Registrar handler para listar ferramentas
server.setRequestHandler(ListToolsRequestSchema, async () => {
  const allTools = [
    ...createContactTools(apiClient),
    ...createReminderTools(apiClient),
    ...createNoteTools(apiClient),
    ...createAutomationTools(apiClient),
    ...createCreditTools(apiClient),
    ...createAgentConfigTools(apiClient),
    ...createCaptureTools(apiClient),
    ...createPlanTools(apiClient),
    ...createEmailTemplateTools(apiClient),
  ];

  return {
    tools: allTools,
  };
});

// Registrar handler para chamar ferramentas
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    let result: any;

    // Roteamento para os handlers apropriados
    if (name.startsWith('list_contacts') || name.startsWith('search_contacts') || 
        name.startsWith('get_contact') || name.startsWith('create_contact') ||
        name.startsWith('update_contact') || name.startsWith('delete_contact') ||
        name.startsWith('add_contact_') || name.startsWith('delete_contact_relationship')) {
      result = await handleContactTool(name, args, apiClient);
    } else if (name.startsWith('create_reminder') || name.startsWith('list_reminders') ||
               name.startsWith('get_reminder') || name.startsWith('update_reminder') ||
               name.startsWith('delete_reminder')) {
      result = await handleReminderTool(name, args, apiClient);
    } else if (name.startsWith('list_contact_notes') || name.startsWith('get_note') ||
               name.startsWith('create_text_note') || name.startsWith('update_note') ||
               name.startsWith('delete_note')) {
      result = await handleNoteTool(name, args, apiClient);
    } else if (name.startsWith('create_draft') || name.startsWith('list_drafts') ||
               name.startsWith('get_draft') || name.startsWith('update_draft') ||
               name.startsWith('approve_draft') || name.startsWith('send_draft') ||
               name.startsWith('delete_draft') || name.startsWith('create_template') ||
               name.startsWith('list_templates') || name.startsWith('get_template') ||
               name.startsWith('update_template') || name.startsWith('delete_template') ||
               name.startsWith('create_letterhead') || name.startsWith('list_letterheads') ||
               name.startsWith('get_letterhead') || name.startsWith('update_letterhead') ||
               name.startsWith('delete_letterhead')) {
      result = await handleAutomationTool(name, args, apiClient);
    } else if (name.startsWith('get_credit_') || name.startsWith('list_credit_') ||
               name.startsWith('purchase_credit_')) {
      result = await handleCreditTool(name, args, apiClient);
    } else if (name.startsWith('get_agent_') || name.startsWith('update_agent_')) {
      result = await handleAgentConfigTool(name, args, apiClient);
    } else if (name.startsWith('get_capture_') || name.startsWith('list_capture_') ||
               name.startsWith('create_audio_note')) {
      result = await handleCaptureTool(name, args, apiClient);
    } else if (name.startsWith('list_plans')) {
      result = await handlePlanTool(name, args, apiClient);
    } else if (name.startsWith('create_email_template') ||
               name.startsWith('list_email_templates') ||
               name.startsWith('get_email_template') ||
               name.startsWith('update_email_template') ||
               name.startsWith('activate_email_template') ||
               name.startsWith('deactivate_email_template') ||
               name.startsWith('delete_email_template')) {
      result = await handleEmailTemplateTool(name, args, apiClient);
    } else {
      throw new McpError(
        ErrorCode.MethodNotFound,
        `Ferramenta desconhecida: ${name}`
      );
    }

    return {
      content: [
        {
          type: 'text',
          text: JSON.stringify(result, null, 2),
        },
      ],
    };
  } catch (error: any) {
    const errorMessage =
      error instanceof Error ? error.message : String(error);
    throw new McpError(
      ErrorCode.InternalError,
      `Erro ao executar ferramenta ${name}: ${errorMessage}`
    );
  }
});

// Iniciar servidor
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.error('Servidor MCP Assistente Executivo iniciado');
  console.error(`API Base URL: ${API_BASE_URL}`);
  console.error(`Token configurado: ${ACCESS_TOKEN ? 'Sim' : 'Não'}`);
}

main().catch((error) => {
  console.error('Erro fatal:', error);
  process.exit(1);
});


