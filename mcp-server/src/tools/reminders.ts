import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createReminderTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'create_reminder',
      description: 'Cria um novo lembrete para um contato. Útil para agendar follow-ups ou tarefas.',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          reason: {
            type: 'string',
            description: 'Motivo do lembrete (máximo 500 caracteres)',
          },
          suggestedMessage: {
            type: 'string',
            description: 'Mensagem sugerida para o lembrete (máximo 2000 caracteres)',
          },
          scheduledFor: {
            type: 'string',
            description: 'Data e hora agendada (formato ISO 8601, ex: 2024-12-25T10:00:00Z)',
          },
        },
        required: ['contactId', 'reason', 'scheduledFor'],
      },
    },
    {
      name: 'list_reminders',
      description: 'Lista lembretes do usuário autenticado. Suporta filtros por contato, status e data.',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'Filtrar por ID do contato (GUID)',
          },
          status: {
            type: 'string',
            description: 'Filtrar por status (Pending, Completed, Cancelled)',
            enum: ['Pending', 'Completed', 'Cancelled'],
          },
          startDate: {
            type: 'string',
            description: 'Data inicial (formato ISO 8601)',
          },
          endDate: {
            type: 'string',
            description: 'Data final (formato ISO 8601)',
          },
          page: {
            type: 'number',
            description: 'Número da página (padrão: 1)',
            default: 1,
          },
          pageSize: {
            type: 'number',
            description: 'Tamanho da página (padrão: 20)',
            default: 20,
          },
        },
      },
    },
    {
      name: 'get_reminder',
      description: 'Obtém um lembrete específico por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do lembrete (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'update_reminder_status',
      description: 'Atualiza o status de um lembrete (Pending, Completed, Cancelled)',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do lembrete (GUID)',
          },
          newStatus: {
            type: 'string',
            description: 'Novo status',
            enum: ['Pending', 'Completed', 'Cancelled'],
          },
          newScheduledFor: {
            type: 'string',
            description: 'Nova data agendada (formato ISO 8601, opcional)',
          },
        },
        required: ['id', 'newStatus'],
      },
    },
    {
      name: 'delete_reminder',
      description: 'Deleta um lembrete',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do lembrete (GUID)',
          },
        },
        required: ['id'],
      },
    },
  ];
}

export async function handleReminderTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'create_reminder':
      return await apiClient.post('/api/automation/reminders', {
        contactId: args.contactId,
        reason: args.reason,
        suggestedMessage: args.suggestedMessage,
        scheduledFor: args.scheduledFor,
      });

    case 'list_reminders':
      return await apiClient.get('/api/automation/reminders', {
        contactId: args.contactId,
        status: args.status,
        startDate: args.startDate,
        endDate: args.endDate,
        page: args.page || 1,
        pageSize: args.pageSize || 20,
      });

    case 'get_reminder':
      return await apiClient.get(`/api/automation/reminders/${args.id}`);

    case 'update_reminder_status':
      return await apiClient.put(`/api/automation/reminders/${args.id}/status`, {
        newStatus: args.newStatus,
        newScheduledFor: args.newScheduledFor,
      });

    case 'delete_reminder':
      await apiClient.delete(`/api/automation/reminders/${args.id}`);
      return { success: true, message: 'Lembrete deletado com sucesso' };

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}

