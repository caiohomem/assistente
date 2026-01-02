import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createAutomationTools(apiClient: ApiClient): Tool[] {
  return [
    // Drafts
    {
      name: 'create_draft',
      description: 'Cria um novo draft de documento',
      inputSchema: {
        type: 'object',
        properties: {
          documentType: {
            type: 'string',
            description: 'Tipo de documento',
            enum: ['Email', 'Letter', 'Proposal', 'Contract', 'Other'],
          },
          content: {
            type: 'string',
            description: 'Conteúdo do documento',
          },
          contactId: {
            type: 'string',
            description: 'ID do contato relacionado (GUID, opcional)',
          },
          companyId: {
            type: 'string',
            description: 'ID da empresa relacionada (GUID, opcional)',
          },
          templateId: {
            type: 'string',
            description: 'ID do template usado (GUID, opcional)',
          },
          letterheadId: {
            type: 'string',
            description: 'ID do papel timbrado usado (GUID, opcional)',
          },
        },
        required: ['documentType', 'content'],
      },
    },
    {
      name: 'list_drafts',
      description: 'Lista drafts do usuário autenticado',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'Filtrar por ID do contato (GUID)',
          },
          companyId: {
            type: 'string',
            description: 'Filtrar por ID da empresa (GUID)',
          },
          documentType: {
            type: 'string',
            description: 'Filtrar por tipo de documento',
            enum: ['Email', 'Letter', 'Proposal', 'Contract', 'Other'],
          },
          status: {
            type: 'string',
            description: 'Filtrar por status',
            enum: ['Draft', 'Approved', 'Sent'],
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
      name: 'get_draft',
      description: 'Obtém um draft específico por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do draft (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'update_draft',
      description: 'Atualiza um draft',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do draft (GUID)',
          },
          content: {
            type: 'string',
            description: 'Conteúdo do documento',
          },
          contactId: {
            type: 'string',
            description: 'ID do contato relacionado (GUID)',
          },
          companyId: {
            type: 'string',
            description: 'ID da empresa relacionada (GUID)',
          },
          templateId: {
            type: 'string',
            description: 'ID do template usado (GUID)',
          },
          letterheadId: {
            type: 'string',
            description: 'ID do papel timbrado usado (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'approve_draft',
      description: 'Aprova um draft',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do draft (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'send_draft',
      description: 'Envia um draft',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do draft (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'delete_draft',
      description: 'Deleta um draft',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do draft (GUID)',
          },
        },
        required: ['id'],
      },
    },
    // Templates
    {
      name: 'create_template',
      description: 'Cria um novo template de documento',
      inputSchema: {
        type: 'object',
        properties: {
          name: {
            type: 'string',
            description: 'Nome do template (máximo 200 caracteres)',
          },
          type: {
            type: 'string',
            description: 'Tipo de template',
            enum: ['Email', 'Letter', 'Proposal', 'Contract', 'Other'],
          },
          body: {
            type: 'string',
            description: 'Corpo do template',
          },
          placeholdersSchema: {
            type: 'string',
            description: 'Schema JSON dos placeholders (opcional)',
          },
        },
        required: ['name', 'type', 'body'],
      },
    },
    {
      name: 'list_templates',
      description: 'Lista templates do usuário autenticado',
      inputSchema: {
        type: 'object',
        properties: {
          type: {
            type: 'string',
            description: 'Filtrar por tipo',
            enum: ['Email', 'Letter', 'Proposal', 'Contract', 'Other'],
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
      name: 'get_template',
      description: 'Obtém um template específico por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'update_template',
      description: 'Atualiza um template',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template (GUID)',
          },
          name: {
            type: 'string',
            description: 'Nome do template',
          },
          body: {
            type: 'string',
            description: 'Corpo do template',
          },
          placeholdersSchema: {
            type: 'string',
            description: 'Schema JSON dos placeholders',
          },
          active: {
            type: 'boolean',
            description: 'Se o template está ativo',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'delete_template',
      description: 'Deleta um template',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do template (GUID)',
          },
        },
        required: ['id'],
      },
    },
    // Letterheads
    {
      name: 'create_letterhead',
      description: 'Cria um novo papel timbrado',
      inputSchema: {
        type: 'object',
        properties: {
          name: {
            type: 'string',
            description: 'Nome do papel timbrado (máximo 200 caracteres)',
          },
          designData: {
            type: 'string',
            description: 'Dados de design em JSON',
          },
        },
        required: ['name', 'designData'],
      },
    },
    {
      name: 'list_letterheads',
      description: 'Lista papéis timbrados do usuário autenticado',
      inputSchema: {
        type: 'object',
        properties: {
          activeOnly: {
            type: 'boolean',
            description: 'Apenas papéis timbrados ativos (padrão: false)',
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
      name: 'get_letterhead',
      description: 'Obtém um papel timbrado específico por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do papel timbrado (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'update_letterhead',
      description: 'Atualiza um papel timbrado',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do papel timbrado (GUID)',
          },
          name: {
            type: 'string',
            description: 'Nome do papel timbrado',
          },
          designData: {
            type: 'string',
            description: 'Dados de design em JSON',
          },
          isActive: {
            type: 'boolean',
            description: 'Se o papel timbrado está ativo',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'delete_letterhead',
      description: 'Deleta um papel timbrado',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do papel timbrado (GUID)',
          },
        },
        required: ['id'],
      },
    },
  ];
}

export async function handleAutomationTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  // Drafts
  if (name.startsWith('create_draft')) {
    return await apiClient.post('/api/automation/drafts', {
      documentType: args.documentType,
      content: args.content,
      contactId: args.contactId,
      companyId: args.companyId,
      templateId: args.templateId,
      letterheadId: args.letterheadId,
    });
  }

  if (name.startsWith('list_drafts')) {
    return await apiClient.get('/api/automation/drafts', {
      contactId: args.contactId,
      companyId: args.companyId,
      documentType: args.documentType,
      status: args.status,
      page: args.page || 1,
      pageSize: args.pageSize || 20,
    });
  }

  if (name.startsWith('get_draft')) {
    return await apiClient.get(`/api/automation/drafts/${args.id}`);
  }

  if (name.startsWith('update_draft')) {
    return await apiClient.put(`/api/automation/drafts/${args.id}`, {
      content: args.content,
      contactId: args.contactId,
      companyId: args.companyId,
      templateId: args.templateId,
      letterheadId: args.letterheadId,
    });
  }

  if (name.startsWith('approve_draft')) {
    await apiClient.post(`/api/automation/drafts/${args.id}/approve`);
    return { success: true, message: 'Draft aprovado com sucesso' };
  }

  if (name.startsWith('send_draft')) {
    await apiClient.post(`/api/automation/drafts/${args.id}/send`);
    return { success: true, message: 'Draft enviado com sucesso' };
  }

  if (name.startsWith('delete_draft')) {
    await apiClient.delete(`/api/automation/drafts/${args.id}`);
    return { success: true, message: 'Draft deletado com sucesso' };
  }

  // Templates
  if (name.startsWith('create_template')) {
    return await apiClient.post('/api/automation/templates', {
      name: args.name,
      type: args.type,
      body: args.body,
      placeholdersSchema: args.placeholdersSchema,
    });
  }

  if (name.startsWith('list_templates')) {
    return await apiClient.get('/api/automation/templates', {
      type: args.type,
      activeOnly: args.activeOnly || false,
      page: args.page || 1,
      pageSize: args.pageSize || 20,
    });
  }

  if (name.startsWith('get_template')) {
    return await apiClient.get(`/api/automation/templates/${args.id}`);
  }

  if (name.startsWith('update_template')) {
    return await apiClient.put(`/api/automation/templates/${args.id}`, {
      name: args.name,
      body: args.body,
      placeholdersSchema: args.placeholdersSchema,
      active: args.active,
    });
  }

  if (name.startsWith('delete_template')) {
    await apiClient.delete(`/api/automation/templates/${args.id}`);
    return { success: true, message: 'Template deletado com sucesso' };
  }

  // Letterheads
  if (name.startsWith('create_letterhead')) {
    return await apiClient.post('/api/automation/letterheads', {
      name: args.name,
      designData: args.designData,
    });
  }

  if (name.startsWith('list_letterheads')) {
    return await apiClient.get('/api/automation/letterheads', {
      activeOnly: args.activeOnly || false,
      page: args.page || 1,
      pageSize: args.pageSize || 20,
    });
  }

  if (name.startsWith('get_letterhead')) {
    return await apiClient.get(`/api/automation/letterheads/${args.id}`);
  }

  if (name.startsWith('update_letterhead')) {
    return await apiClient.put(`/api/automation/letterheads/${args.id}`, {
      name: args.name,
      designData: args.designData,
      isActive: args.isActive,
    });
  }

  if (name.startsWith('delete_letterhead')) {
    await apiClient.delete(`/api/automation/letterheads/${args.id}`);
    return { success: true, message: 'Papel timbrado deletado com sucesso' };
  }

  throw new Error(`Ferramenta desconhecida: ${name}`);
}









