import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createPlanTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'list_plans',
      description: 'Lista todos os planos disponíveis (endpoint público, não requer autenticação)',
      inputSchema: {
        type: 'object',
        properties: {
          includeInactive: {
            type: 'boolean',
            description: 'Incluir planos inativos (padrão: false)',
            default: false,
          },
        },
      },
    },
  ];
}

export async function handlePlanTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'list_plans':
      return await apiClient.get('/api/plans', {
        includeInactive: args.includeInactive || false,
      });

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}





