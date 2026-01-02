import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createAgentConfigTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'get_agent_configuration',
      description: 'Obtém a configuração atual do agente (prompts de OCR e transcrição)',
      inputSchema: {
        type: 'object',
        properties: {},
      },
    },
    {
      name: 'update_agent_configuration',
      description: 'Atualiza ou cria a configuração do agente',
      inputSchema: {
        type: 'object',
        properties: {
          ocrPrompt: {
            type: 'string',
            description: 'Prompt para OCR (processamento de imagens)',
          },
          transcriptionPrompt: {
            type: 'string',
            description: 'Prompt para transcrição de áudio',
          },
        },
      },
    },
  ];
}

export async function handleAgentConfigTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'get_agent_configuration':
      return await apiClient.get('/api/agent-configuration');

    case 'update_agent_configuration':
      return await apiClient.put('/api/agent-configuration', {
        ocrPrompt: args.ocrPrompt,
        transcriptionPrompt: args.transcriptionPrompt,
      });

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}









