// Tipos principais da API

export interface Contact {
  contactId: string;
  ownerUserId: string;
  firstName: string;
  lastName?: string;
  jobTitle?: string;
  company?: string;
  street?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  emails: ContactEmail[];
  phones: ContactPhone[];
  tags: string[];
  relationships: ContactRelationship[];
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
}

export interface ContactEmail {
  emailId: string;
  email: string;
  createdAt: string;
}

export interface ContactPhone {
  phoneId: string;
  phone: string;
  createdAt: string;
}

export interface ContactRelationship {
  relationshipId: string;
  targetContactId: string;
  type: string;
  description?: string;
  createdAt: string;
}

export interface CreateContactRequest {
  firstName: string;
  lastName?: string;
  jobTitle?: string;
  company?: string;
  street?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
}

export interface UpdateContactRequest {
  firstName?: string;
  lastName?: string;
  jobTitle?: string;
  company?: string;
  street?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
}

export interface Reminder {
  reminderId: string;
  ownerUserId: string;
  contactId: string;
  reason: string;
  suggestedMessage?: string;
  scheduledFor: string;
  status: ReminderStatus;
  createdAt: string;
  updatedAt: string;
}

export enum ReminderStatus {
  Pending = "Pending",
  Completed = "Completed",
  Cancelled = "Cancelled",
  Snoozed = "Snoozed"
}

export interface CreateReminderRequest {
  contactId: string;
  reason: string;
  suggestedMessage?: string;
  scheduledFor: string;
}

export interface UpdateReminderStatusRequest {
  newStatus: ReminderStatus;
  newScheduledFor?: string;
}

export interface Note {
  noteId: string;
  contactId: string;
  authorId: string;
  type: NoteType;
  rawContent?: string;
  structuredData?: string;
  createdAt: string;
  updatedAt: string;
}

export enum NoteType {
  Text = "Text",
  Audio = "Audio"
}

export interface CreateTextNoteRequest {
  text: string;
  structuredData?: string;
}

export interface UpdateNoteRequest {
  rawContent?: string;
  structuredData?: string;
}

export interface Draft {
  draftId: string;
  ownerUserId: string;
  documentType: DocumentType;
  content: string;
  contactId?: string;
  companyId?: string;
  templateId?: string;
  letterheadId?: string;
  status: DraftStatus;
  createdAt: string;
  updatedAt: string;
}

export enum DocumentType {
  Letter = "Letter",
  Email = "Email",
  Contract = "Contract",
  Other = "Other"
}

export enum DraftStatus {
  Draft = "Draft",
  Approved = "Approved",
  Sent = "Sent"
}

export interface Template {
  templateId: string;
  ownerUserId: string;
  name: string;
  type: TemplateType;
  body: string;
  placeholdersSchema?: string;
  active: boolean;
  createdAt: string;
  updatedAt: string;
}

export enum TemplateType {
  Letter = "Letter",
  Email = "Email",
  Contract = "Contract",
  Other = "Other"
}

export interface Letterhead {
  letterheadId: string;
  ownerUserId: string;
  name: string;
  designData: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreditBalance {
  ownerUserId: string;
  balance: number;
  createdAt: string;
  transactionCount: number;
}

export interface CreditTransaction {
  transactionId: string;
  ownerUserId: string;
  type: CreditTransactionType;
  amount: number;
  reason?: string;
  createdAt: string;
}

export enum CreditTransactionType {
  Purchase = "Purchase",
  Usage = "Usage",
  Grant = "Grant",
  Refund = "Refund"
}

export interface Plan {
  planId: string;
  name: string;
  description?: string;
  price: number;
  creditAmount: number;
  active: boolean;
  createdAt: string;
}

export interface CreditPackage {
  packageId: string;
  name: string;
  description?: string;
  creditAmount: number;
  price: number;
  active: boolean;
  createdAt: string;
}

export interface ListResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiError {
  message: string;
  error?: string;
  statusCode?: number;
}

export interface SessionInfo {
  authenticated: boolean;
  user?: {
    sub: string;
    email: string;
    name: string;
    givenName?: string;
    familyName?: string;
  };
  csrfToken?: string;
  expiresAtUnix?: number;
}

