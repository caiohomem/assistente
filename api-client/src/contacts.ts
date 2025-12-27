import { HttpClient } from "./http-client";
import {
  Contact,
  CreateContactRequest,
  UpdateContactRequest,
  ListResult,
} from "./types";

export class ContactsService {
  constructor(private http: HttpClient) {}

  /**
   * Lista todos os contatos do usuário autenticado
   */
  async list(options?: {
    page?: number;
    pageSize?: number;
    includeDeleted?: boolean;
  }): Promise<ListResult<Contact>> {
    const params: any = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
      includeDeleted: options?.includeDeleted || false,
    };
    return this.http.get<ListResult<Contact>>("/api/contacts", params);
  }

  /**
   * Busca contatos com filtro de texto
   */
  async search(
    searchTerm: string,
    options?: {
      page?: number;
      pageSize?: number;
    }
  ): Promise<ListResult<Contact>> {
    const params: any = {
      searchTerm,
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
    };
    return this.http.get<ListResult<Contact>>("/api/contacts/search", params);
  }

  /**
   * Obtém um contato específico por ID
   */
  async getById(contactId: string): Promise<Contact> {
    return this.http.get<Contact>(`/api/contacts/${contactId}`);
  }

  /**
   * Cria um novo contato
   */
  async create(data: CreateContactRequest): Promise<{ contactId: string }> {
    return this.http.post<{ contactId: string }>("/api/contacts", data);
  }

  /**
   * Atualiza um contato existente
   */
  async update(contactId: string, data: UpdateContactRequest): Promise<void> {
    return this.http.put(`/api/contacts/${contactId}`, data);
  }

  /**
   * Deleta um contato (soft delete)
   */
  async delete(contactId: string): Promise<void> {
    return this.http.delete(`/api/contacts/${contactId}`);
  }

  /**
   * Adiciona um email a um contato
   */
  async addEmail(contactId: string, email: string): Promise<void> {
    return this.http.post(`/api/contacts/${contactId}/emails`, { email });
  }

  /**
   * Adiciona um telefone a um contato
   */
  async addPhone(contactId: string, phone: string): Promise<void> {
    return this.http.post(`/api/contacts/${contactId}/phones`, { phone });
  }

  /**
   * Adiciona uma tag a um contato
   */
  async addTag(contactId: string, tag: string): Promise<void> {
    return this.http.post(`/api/contacts/${contactId}/tags`, { tag });
  }

  /**
   * Adiciona um relacionamento entre contatos
   */
  async addRelationship(
    contactId: string,
    targetContactId: string,
    type: string,
    description?: string
  ): Promise<void> {
    return this.http.post(`/api/contacts/${contactId}/relationships`, {
      targetContactId,
      type,
      description,
    });
  }

  /**
   * Deleta um relacionamento
   */
  async deleteRelationship(relationshipId: string): Promise<void> {
    return this.http.delete(`/api/contacts/relationships/${relationshipId}`);
  }
}

