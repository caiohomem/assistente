// Enums
export enum ReminderStatus {
  Pending = 1,
  Sent = 2,
  Dismissed = 3,
  Snoozed = 4
}

export enum DraftStatus {
  Draft = 1,
  Approved = 2,
  Sent = 3
}

export enum TemplateType {
  Email = 1,
  Oficio = 2,
  Invite = 3,
  Generic = 4
}

export enum DocumentType {
  Email = 1,
  Oficio = 2,
  Invite = 3
}

// Reminder Types
export interface Reminder {
  reminderId: string;
  ownerUserId: string;
  contactId: string;
  reason: string;
  suggestedMessage?: string | null;
  scheduledFor: string;
  status: ReminderStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReminderRequest {
  contactId: string;
  reason: string;
  suggestedMessage?: string | null;
  scheduledFor: string; // ISO date string
}

export interface UpdateReminderStatusRequest {
  newStatus: ReminderStatus;
  newScheduledFor?: string | null; // ISO date string
}

export interface ListRemindersResult {
  reminders: Reminder[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Draft Document Types
export interface DraftDocument {
  draftId: string;
  ownerUserId: string;
  contactId?: string | null;
  companyId?: string | null;
  documentType: DocumentType;
  templateId?: string | null;
  letterheadId?: string | null;
  content: string;
  status: DraftStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDraftRequest {
  documentType: DocumentType;
  content: string;
  contactId?: string | null;
  companyId?: string | null;
  templateId?: string | null;
  letterheadId?: string | null;
}

export interface UpdateDraftRequest {
  content?: string | null;
  contactId?: string | null;
  companyId?: string | null;
  templateId?: string | null;
  letterheadId?: string | null;
}

export interface ListDraftsResult {
  drafts: DraftDocument[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Template Types
export interface Template {
  templateId: string;
  ownerUserId: string;
  name: string;
  type: TemplateType;
  body: string;
  placeholdersSchema?: string | null;
  active: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTemplateRequest {
  name: string;
  type: TemplateType;
  body: string;
  placeholdersSchema?: string | null;
}

export interface UpdateTemplateRequest {
  name?: string | null;
  body?: string | null;
  placeholdersSchema?: string | null;
  active?: boolean | null;
}

export interface ListTemplatesResult {
  templates: Template[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Letterhead Types
export interface Letterhead {
  letterheadId: string;
  ownerUserId: string;
  name: string;
  designData: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLetterheadRequest {
  name: string;
  designData: string;
}

export interface UpdateLetterheadRequest {
  name?: string | null;
  designData?: string | null;
  isActive?: boolean | null;
}

export interface ListLetterheadsResult {
  letterheads: Letterhead[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}









