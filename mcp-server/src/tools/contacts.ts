import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';

export function createContactTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'list_contacts',
      description: 'Lista todos os contatos do usuário autenticado. Suporta paginação e filtros.',
      inputSchema: {
        type: 'object',
        properties: {
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
          includeDeleted: {
            type: 'boolean',
            description: 'Incluir contatos deletados (padrão: false)',
            default: false,
          },
        },
      },
    },
    {
      name: 'search_contacts',
      description: 'Busca contatos com filtro de texto. Útil para encontrar contatos por nome, empresa, etc.',
      inputSchema: {
        type: 'object',
        properties: {
          searchTerm: {
            type: 'string',
            description: 'Termo de busca (nome, empresa, email, etc)',
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
      name: 'get_contact',
      description: 'Obtém um contato específico por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'create_contact',
      description: 'Cria um novo contato. Primeiro nome é obrigatório, outros campos são opcionais.',
      inputSchema: {
        type: 'object',
        properties: {
          firstName: {
            type: 'string',
            description: 'Primeiro nome (obrigatório)',
          },
          lastName: {
            type: 'string',
            description: 'Sobrenome',
          },
          jobTitle: {
            type: 'string',
            description: 'Cargo',
          },
          company: {
            type: 'string',
            description: 'Empresa',
          },
          street: {
            type: 'string',
            description: 'Endereço (rua)',
          },
          city: {
            type: 'string',
            description: 'Cidade',
          },
          state: {
            type: 'string',
            description: 'Estado',
          },
          zipCode: {
            type: 'string',
            description: 'CEP',
          },
          country: {
            type: 'string',
            description: 'País',
          },
        },
        required: ['firstName'],
      },
    },
    {
      name: 'update_contact',
      description: 'Atualiza um contato existente. Todos os campos são opcionais.',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          firstName: {
            type: 'string',
            description: 'Primeiro nome',
          },
          lastName: {
            type: 'string',
            description: 'Sobrenome',
          },
          jobTitle: {
            type: 'string',
            description: 'Cargo',
          },
          company: {
            type: 'string',
            description: 'Empresa',
          },
          street: {
            type: 'string',
            description: 'Endereço (rua)',
          },
          city: {
            type: 'string',
            description: 'Cidade',
          },
          state: {
            type: 'string',
            description: 'Estado',
          },
          zipCode: {
            type: 'string',
            description: 'CEP',
          },
          country: {
            type: 'string',
            description: 'País',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'delete_contact',
      description: 'Deleta um contato (soft delete)',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'add_contact_email',
      description: 'Adiciona um email a um contato',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          email: {
            type: 'string',
            description: 'Endereço de email',
          },
        },
        required: ['contactId', 'email'],
      },
    },
    {
      name: 'add_contact_phone',
      description: 'Adiciona um telefone a um contato',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          phone: {
            type: 'string',
            description: 'Número de telefone',
          },
        },
        required: ['contactId', 'phone'],
      },
    },
    {
      name: 'add_contact_tag',
      description: 'Adiciona uma tag a um contato',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          tag: {
            type: 'string',
            description: 'Nome da tag',
          },
        },
        required: ['contactId', 'tag'],
      },
    },
    {
      name: 'add_contact_relationship',
      description: 'Adiciona um relacionamento entre contatos',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato de origem (GUID)',
          },
          targetContactId: {
            type: 'string',
            description: 'ID do contato de destino (GUID)',
          },
          type: {
            type: 'string',
            description: 'Tipo de relacionamento (ex: "colleague", "friend", "family")',
          },
          description: {
            type: 'string',
            description: 'Descrição do relacionamento',
          },
        },
        required: ['contactId', 'targetContactId', 'type'],
      },
    },
    {
      name: 'delete_contact_relationship',
      description: 'Deleta um relacionamento entre contatos',
      inputSchema: {
        type: 'object',
        properties: {
          relationshipId: {
            type: 'string',
            description: 'ID do relacionamento (GUID)',
          },
        },
        required: ['relationshipId'],
      },
    },
  ];
}

export async function handleContactTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'list_contacts':
      return await apiClient.get('/api/contacts', {
        page: args.page || 1,
        pageSize: args.pageSize || 20,
        includeDeleted: args.includeDeleted || false,
      });

    case 'search_contacts':
      return await apiClient.get('/api/contacts/search', {
        searchTerm: args.searchTerm,
        page: args.page || 1,
        pageSize: args.pageSize || 20,
      });

    case 'get_contact':
      return await apiClient.get(`/api/contacts/${args.id}`);

    case 'create_contact':
      return await apiClient.post('/api/contacts', {
        firstName: args.firstName,
        lastName: args.lastName,
        jobTitle: args.jobTitle,
        company: args.company,
        street: args.street,
        city: args.city,
        state: args.state,
        zipCode: args.zipCode,
        country: args.country,
      });

    case 'update_contact':
      return await apiClient.put(`/api/contacts/${args.id}`, {
        firstName: args.firstName,
        lastName: args.lastName,
        jobTitle: args.jobTitle,
        company: args.company,
        street: args.street,
        city: args.city,
        state: args.state,
        zipCode: args.zipCode,
        country: args.country,
      });

    case 'delete_contact':
      await apiClient.delete(`/api/contacts/${args.id}`);
      return { success: true, message: 'Contato deletado com sucesso' };

    case 'add_contact_email':
      await apiClient.post(`/api/contacts/${args.contactId}/emails`, {
        email: args.email,
      });
      return { success: true, message: 'Email adicionado com sucesso' };

    case 'add_contact_phone':
      await apiClient.post(`/api/contacts/${args.contactId}/phones`, {
        phone: args.phone,
      });
      return { success: true, message: 'Telefone adicionado com sucesso' };

    case 'add_contact_tag':
      await apiClient.post(`/api/contacts/${args.contactId}/tags`, {
        tag: args.tag,
      });
      return { success: true, message: 'Tag adicionada com sucesso' };

    case 'add_contact_relationship':
      await apiClient.post(`/api/contacts/${args.contactId}/relationships`, {
        targetContactId: args.targetContactId,
        type: args.type,
        description: args.description,
      });
      return { success: true, message: 'Relacionamento adicionado com sucesso' };

    case 'delete_contact_relationship':
      await apiClient.delete(`/api/contacts/relationships/${args.relationshipId}`);
      return { success: true, message: 'Relacionamento deletado com sucesso' };

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}





