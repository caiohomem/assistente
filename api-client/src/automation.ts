import { HttpClient } from "./http-client";
import {
  Draft,
  Template,
  Letterhead,
  DocumentType,
  DraftStatus,
  TemplateType,
  ListResult,
} from "./types";

export class AutomationService {
  constructor(private http: HttpClient) {}

  // ========== DRAFTS ==========

  /**
   * Lista drafts do usuário autenticado
   */
  async listDrafts(options?: {
    contactId?: string;
    companyId?: string;
    documentType?: DocumentType;
    status?: DraftStatus;
    page?: number;
    pageSize?: number;
  }): Promise<ListResult<Draft>> {
    const params: any = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
    };

    if (options?.contactId) {
      params.contactId = options.contactId;
    }
    if (options?.companyId) {
      params.companyId = options.companyId;
    }
    if (options?.documentType) {
      params.documentType = options.documentType;
    }
    if (options?.status) {
      params.status = options.status;
    }

    return this.http.get<ListResult<Draft>>("/api/automation/drafts", params);
  }

  /**
   * Obtém um draft específico por ID
   */
  async getDraftById(draftId: string): Promise<Draft> {
    return this.http.get<Draft>(`/api/automation/drafts/${draftId}`);
  }

  /**
   * Cria um novo draft de documento
   */
  async createDraft(data: {
    documentType: DocumentType;
    content: string;
    contactId?: string;
    companyId?: string;
    templateId?: string;
    letterheadId?: string;
  }): Promise<{ draftId: string }> {
    const response = await this.http.post<string>("/api/automation/drafts", data);
    return { draftId: response };
  }

  /**
   * Atualiza um draft
   */
  async updateDraft(
    draftId: string,
    data: {
      content?: string;
      contactId?: string;
      companyId?: string;
      templateId?: string;
      letterheadId?: string;
    }
  ): Promise<void> {
    return this.http.put(`/api/automation/drafts/${draftId}`, data);
  }

  /**
   * Aprova um draft
   */
  async approveDraft(draftId: string): Promise<void> {
    return this.http.post(`/api/automation/drafts/${draftId}/approve`);
  }

  /**
   * Envia um draft
   */
  async sendDraft(draftId: string): Promise<void> {
    return this.http.post(`/api/automation/drafts/${draftId}/send`);
  }

  /**
   * Deleta um draft
   */
  async deleteDraft(draftId: string): Promise<void> {
    return this.http.delete(`/api/automation/drafts/${draftId}`);
  }

  // ========== TEMPLATES ==========

  /**
   * Lista templates do usuário autenticado
   */
  async listTemplates(options?: {
    type?: TemplateType;
    activeOnly?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<ListResult<Template>> {
    const params: any = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
      activeOnly: options?.activeOnly || false,
    };

    if (options?.type) {
      params.type = options.type;
    }

    return this.http.get<ListResult<Template>>("/api/automation/templates", params);
  }

  /**
   * Obtém um template específico por ID
   */
  async getTemplateById(templateId: string): Promise<Template> {
    return this.http.get<Template>(`/api/automation/templates/${templateId}`);
  }

  /**
   * Cria um novo template
   */
  async createTemplate(data: {
    name: string;
    type: TemplateType;
    body: string;
    placeholdersSchema?: string;
  }): Promise<{ templateId: string }> {
    const response = await this.http.post<string>("/api/automation/templates", data);
    return { templateId: response };
  }

  /**
   * Atualiza um template
   */
  async updateTemplate(
    templateId: string,
    data: {
      name?: string;
      body?: string;
      placeholdersSchema?: string;
      active?: boolean;
    }
  ): Promise<void> {
    return this.http.put(`/api/automation/templates/${templateId}`, data);
  }

  /**
   * Deleta um template
   */
  async deleteTemplate(templateId: string): Promise<void> {
    return this.http.delete(`/api/automation/templates/${templateId}`);
  }

  // ========== LETTERHEADS ==========

  /**
   * Lista papéis timbrados do usuário autenticado
   */
  async listLetterheads(options?: {
    activeOnly?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<ListResult<Letterhead>> {
    const params: any = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
      activeOnly: options?.activeOnly || false,
    };

    return this.http.get<ListResult<Letterhead>>("/api/automation/letterheads", params);
  }

  /**
   * Obtém um papel timbrado específico por ID
   */
  async getLetterheadById(letterheadId: string): Promise<Letterhead> {
    return this.http.get<Letterhead>(`/api/automation/letterheads/${letterheadId}`);
  }

  /**
   * Cria um novo papel timbrado
   */
  async createLetterhead(data: {
    name: string;
    designData: string;
  }): Promise<{ letterheadId: string }> {
    const response = await this.http.post<string>("/api/automation/letterheads", data);
    return { letterheadId: response };
  }

  /**
   * Atualiza um papel timbrado
   */
  async updateLetterhead(
    letterheadId: string,
    data: {
      name?: string;
      designData?: string;
      isActive?: boolean;
    }
  ): Promise<void> {
    return this.http.put(`/api/automation/letterheads/${letterheadId}`, data);
  }

  /**
   * Deleta um papel timbrado
   */
  async deleteLetterhead(letterheadId: string): Promise<void> {
    return this.http.delete(`/api/automation/letterheads/${letterheadId}`);
  }
}

