import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createNoteTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'list_contact_notes',
      description: 'Lista todas as notas de um contato específico',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
        },
        required: ['contactId'],
      },
    },
    {
      name: 'get_note',
      description: 'Obtém uma nota específica por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID da nota (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'create_text_note',
      description: 'Cria uma nova nota de texto para um contato',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          text: {
            type: 'string',
            description: 'Conteúdo da nota',
          },
          structuredData: {
            type: 'string',
            description: 'Dados estruturados em JSON (opcional)',
          },
        },
        required: ['contactId', 'text'],
      },
    },
    {
      name: 'update_note',
      description: 'Atualiza uma nota existente',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID da nota (GUID)',
          },
          rawContent: {
            type: 'string',
            description: 'Conteúdo bruto da nota',
          },
          structuredData: {
            type: 'string',
            description: 'Dados estruturados em JSON',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'delete_note',
      description: 'Deleta uma nota',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID da nota (GUID)',
          },
        },
        required: ['id'],
      },
    },
  ];
}

export async function handleNoteTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'list_contact_notes':
      return await apiClient.get(`/api/contacts/${args.contactId}/notes`);

    case 'get_note':
      return await apiClient.get(`/api/notes/${args.id}`);

    case 'create_text_note':
      return await apiClient.post(`/api/contacts/${args.contactId}/notes`, {
        text: args.text,
        structuredData: args.structuredData,
      });

    case 'update_note':
      return await apiClient.put(`/api/notes/${args.id}`, {
        rawContent: args.rawContent,
        structuredData: args.structuredData,
      });

    case 'delete_note':
      await apiClient.delete(`/api/notes/${args.id}`);
      return { success: true, message: 'Nota deletada com sucesso' };

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}









