import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createEmailTemplateTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'create_email_template',
      description: 'Cria um novo template de email do sistema',
      inputSchema: {
        type: 'object',
        properties: {
          name: {
            type: 'string',
            description: 'Nome do template (máximo 200 caracteres)',
          },
          templateType: {
            type: 'string',
            description: 'Tipo de template de email',
            enum: ['UserCreated', 'PasswordReset', 'Welcome'],
          },
          subject: {
            type: 'string',
            description: 'Assunto do email',
          },
          htmlBody: {
            type: 'string',
            description: 'Corpo HTML do email',
          },
        },
        required: ['name', 'templateType', 'subject', 'htmlBody'],
      },
    },
    {
      name: 'list_email_templates',
      description: 'Lista templates de email do sistema',
      inputSchema: {
        type: 'object',
        properties: {
          templateType: {
            type: 'string',
            description: 'Filtrar por tipo',
            enum: ['UserCreated', 'PasswordReset', 'Welcome'],
          },
          activeOnly: {
            type: 'boolean',
            description: 'Apenas templates ativos (padrão: false)',
            default: false,
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
      name: 'get_email_template',
      description: 'Obtém um template de email específico por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template de email (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'update_email_template',
      description: 'Atualiza um template de email',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template de email (GUID)',
          },
          name: {
            type: 'string',
            description: 'Nome do template',
          },
          subject: {
            type: 'string',
            description: 'Assunto do email',
          },
          htmlBody: {
            type: 'string',
            description: 'Corpo HTML do email',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'activate_email_template',
      description: 'Ativa um template de email',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template de email (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'deactivate_email_template',
      description: 'Desativa um template de email',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template de email (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'delete_email_template',
      description: 'Deleta um template de email',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template de email (GUID)',
          },
        },
        required: ['id'],
      },
    },
  ];
}

export async function handleEmailTemplateTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  if (name.startsWith('create_email_template')) {
    return await apiClient.post('/api/email-templates', {
      name: args.name,
      templateType: args.templateType,
      subject: args.subject,
      htmlBody: args.htmlBody,
    });
  }

  if (name.startsWith('list_email_templates')) {
    return await apiClient.get('/api/email-templates', {
      templateType: args.templateType,
      activeOnly: args.activeOnly || false,
      page: args.page || 1,
      pageSize: args.pageSize || 20,
    });
  }

  if (name.startsWith('get_email_template')) {
    return await apiClient.get(`/api/email-templates/${args.id}`);
  }

  if (name.startsWith('update_email_template')) {
    return await apiClient.put(`/api/email-templates/${args.id}`, {
      name: args.name,
      subject: args.subject,
      htmlBody: args.htmlBody,
    });
  }

  if (name.startsWith('activate_email_template')) {
    await apiClient.post(`/api/email-templates/${args.id}/activate`);
    return { success: true, message: 'Template de email ativado com sucesso' };
  }

  if (name.startsWith('deactivate_email_template')) {
    await apiClient.post(`/api/email-templates/${args.id}/deactivate`);
    return { success: true, message: 'Template de email desativado com sucesso' };
  }

  if (name.startsWith('delete_email_template')) {
    await apiClient.delete(`/api/email-templates/${args.id}`);
    return { success: true, message: 'Template de email deletado com sucesso' };
  }

  throw new Error(`Ferramenta desconhecida: ${name}`);
}

