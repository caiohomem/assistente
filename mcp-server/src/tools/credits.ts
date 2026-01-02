import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createCreditTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'get_credit_balance',
      description: 'Obtém o saldo de créditos do usuário autenticado',
      inputSchema: {
        type: 'object',
        properties: {},
      },
    },
    {
      name: 'list_credit_transactions',
      description: 'Lista as transações de crédito do usuário autenticado',
      inputSchema: {
        type: 'object',
        properties: {
          type: {
            type: 'string',
            description: 'Filtrar por tipo de transação',
            enum: ['Purchase', 'Usage', 'Grant', 'Refund'],
          },
          fromDate: {
            type: 'string',
            description: 'Data inicial (formato ISO 8601)',
          },
          toDate: {
            type: 'string',
            description: 'Data final (formato ISO 8601)',
          },
          limit: {
            type: 'number',
            description: 'Limite de resultados',
          },
          offset: {
            type: 'number',
            description: 'Offset para paginação',
          },
        },
      },
    },
    {
      name: 'list_credit_packages',
      description: 'Lista os pacotes de créditos disponíveis',
      inputSchema: {
        type: 'object',
        properties: {
          includeInactive: {
            type: 'boolean',
            description: 'Incluir pacotes inativos (padrão: false)',
            default: false,
          },
        },
      },
    },
    {
      name: 'purchase_credit_package',
      description: 'Compra um pacote de créditos',
      inputSchema: {
        type: 'object',
        properties: {
          packageId: {
            type: 'string',
            description: 'ID do pacote de créditos (GUID)',
          },
        },
        required: ['packageId'],
      },
    },
  ];
}

export async function handleCreditTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'get_credit_balance':
      return await apiClient.get('/api/credits/balance');

    case 'list_credit_transactions':
      return await apiClient.get('/api/credits/transactions', {
        type: args.type,
        fromDate: args.fromDate,
        toDate: args.toDate,
        limit: args.limit,
        offset: args.offset,
      });

    case 'list_credit_packages':
      return await apiClient.get('/api/credits/packages', {
        includeInactive: args.includeInactive || false,
      });

    case 'purchase_credit_package':
      return await apiClient.post('/api/credits/purchase', {
        packageId: args.packageId,
      });

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}









