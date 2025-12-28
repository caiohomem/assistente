export enum EmailTemplateType {
  UserCreated = 1,
  PasswordReset = 2,
  Welcome = 3,
}

export interface EmailTemplate {
  id: string;
  name: string;
  templateType: EmailTemplateType;
  subject: string;
  htmlBody: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string | null;
  placeholders: string[];
}

export interface CreateEmailTemplateRequest {
  name: string;
  templateType: EmailTemplateType;
  subject: string;
  htmlBody: string;
}

export interface UpdateEmailTemplateRequest {
  name?: string;
  subject?: string;
  htmlBody?: string;
}

export interface ListEmailTemplatesResult {
  templates: EmailTemplate[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ListEmailTemplatesParams {
  templateType?: EmailTemplateType;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
}

