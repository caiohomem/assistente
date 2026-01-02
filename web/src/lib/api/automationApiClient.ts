"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import { throwIfErrorResponse } from "./types";
import type {
  Reminder,
  CreateReminderRequest,
  UpdateReminderStatusRequest,
  ListRemindersResult,
  DraftDocument,
  CreateDraftRequest,
  UpdateDraftRequest,
  ListDraftsResult,
  Template,
  CreateTemplateRequest,
  UpdateTemplateRequest,
  ListTemplatesResult,
  Letterhead,
  CreateLetterheadRequest,
  UpdateLetterheadRequest,
  ListLetterheadsResult,
  ReminderStatus,
  DraftStatus,
  TemplateType,
  DocumentType,
} from "@/lib/types/automation";

// Re-export types for convenience
export type {
  Reminder,
  ListRemindersResult,
  CreateReminderRequest,
  UpdateReminderStatusRequest,
  DraftDocument,
  ListDraftsResult,
  CreateDraftRequest,
  UpdateDraftRequest,
  Template,
  ListTemplatesResult,
  CreateTemplateRequest,
  UpdateTemplateRequest,
  Letterhead,
  ListLetterheadsResult,
  CreateLetterheadRequest,
  UpdateLetterheadRequest,
};

// ============================================================================
// REMINDERS
// ============================================================================

export interface ListRemindersParams {
  contactId?: string;
  status?: ReminderStatus;
  startDate?: string; // ISO date string
  endDate?: string; // ISO date string
  page?: number;
  pageSize?: number;
}

export async function listRemindersClient(
  params: ListRemindersParams = {},
): Promise<ListRemindersResult> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.contactId) queryParams.set("contactId", params.contactId);
  if (params.status !== undefined) queryParams.set("status", params.status.toString());
  if (params.startDate) queryParams.set("startDate", params.startDate);
  if (params.endDate) queryParams.set("endDate", params.endDate);
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/automation/reminders${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
    },
  });

  if (res.status === 204) {
    return {
      reminders: [],
      total: 0,
      page: params.page ?? 1,
      pageSize: params.pageSize ?? 20,
      totalPages: 0,
    };
  }
  await throwIfErrorResponse(res);
  const data = (await res.json()) as {
    reminders?: Reminder[];
    Reminders?: Reminder[];
    total?: number;
    Total?: number;
    page?: number;
    Page?: number;
    pageSize?: number;
    PageSize?: number;
    totalPages?: number;
    TotalPages?: number;
  };
  return {
    reminders: data.reminders ?? data.Reminders ?? [],
    total: data.total ?? data.Total ?? 0,
    page: data.page ?? data.Page ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? 20,
    totalPages: data.totalPages ?? data.TotalPages ?? 0,
  };
}

export async function getReminderByIdClient(reminderId: string): Promise<Reminder> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/reminders/${reminderId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  await throwIfErrorResponse(res);

  return (await res.json()) as Reminder;
}

export async function createReminderClient(
  request: CreateReminderRequest,
): Promise<{ reminderId: string }> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/reminders`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  await throwIfErrorResponse(res);

  const reminderId = await res.text();
  return { reminderId: reminderId.replace(/"/g, "") };
}

export async function updateReminderStatusClient(
  reminderId: string,
  request: UpdateReminderStatusRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/reminders/${reminderId}/status`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  if (res.status === 204) {
    return;
  }

  await throwIfErrorResponse(res);
}

// ============================================================================
// DRAFT DOCUMENTS
// ============================================================================

export interface ListDraftsParams {
  contactId?: string;
  companyId?: string;
  documentType?: DocumentType;
  status?: DraftStatus;
  page?: number;
  pageSize?: number;
}

export async function listDraftsClient(
  params: ListDraftsParams = {},
): Promise<ListDraftsResult> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.contactId) queryParams.set("contactId", params.contactId);
  if (params.companyId) queryParams.set("companyId", params.companyId);
  if (params.documentType !== undefined) queryParams.set("documentType", params.documentType.toString());
  if (params.status !== undefined) queryParams.set("status", params.status.toString());
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/automation/drafts${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
    },
  });

  await throwIfErrorResponse(res);
  const data = (await res.json()) as {
    drafts?: DraftDocument[];
    Drafts?: DraftDocument[];
    total?: number;
    Total?: number;
    page?: number;
    Page?: number;
    pageSize?: number;
    PageSize?: number;
    totalPages?: number;
    TotalPages?: number;
  };
  return {
    drafts: data.drafts ?? data.Drafts ?? [],
    total: data.total ?? data.Total ?? 0,
    page: data.page ?? data.Page ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? 20,
    totalPages: data.totalPages ?? data.TotalPages ?? 0,
  };
}

export async function getDraftByIdClient(draftId: string): Promise<DraftDocument> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/drafts/${draftId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  await throwIfErrorResponse(res);

  return (await res.json()) as DraftDocument;
}

export async function createDraftClient(
  request: CreateDraftRequest,
): Promise<{ draftId: string }> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/drafts`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  await throwIfErrorResponse(res);

  const draftId = await res.text();
  return { draftId: draftId.replace(/"/g, "") };
}

export async function updateDraftClient(
  draftId: string,
  request: UpdateDraftRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/drafts/${draftId}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

export async function approveDraftClient(draftId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/drafts/${draftId}/approve`, {
    method: "POST",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

export async function sendDraftClient(draftId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/drafts/${draftId}/send`, {
    method: "POST",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

// ============================================================================
// TEMPLATES
// ============================================================================

export interface ListTemplatesParams {
  type?: TemplateType;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export async function listTemplatesClient(
  params: ListTemplatesParams = {},
): Promise<ListTemplatesResult> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.type !== undefined) queryParams.set("type", params.type.toString());
  if (params.activeOnly) queryParams.set("activeOnly", "true");
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/automation/templates${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
    },
  });

  await throwIfErrorResponse(res);
  const data = (await res.json()) as {
    templates?: Template[];
    Templates?: Template[];
    total?: number;
    Total?: number;
    page?: number;
    Page?: number;
    pageSize?: number;
    PageSize?: number;
    totalPages?: number;
    TotalPages?: number;
  };
  return {
    templates: data.templates ?? data.Templates ?? [],
    total: data.total ?? data.Total ?? 0,
    page: data.page ?? data.Page ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? 20,
    totalPages: data.totalPages ?? data.TotalPages ?? 0,
  };
}

export async function getTemplateByIdClient(templateId: string): Promise<Template> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/templates/${templateId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  await throwIfErrorResponse(res);

  return (await res.json()) as Template;
}

export async function createTemplateClient(
  request: CreateTemplateRequest,
): Promise<{ templateId: string }> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/templates`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  await throwIfErrorResponse(res);

  const templateId = await res.text();
  return { templateId: templateId.replace(/"/g, "") };
}

export async function updateTemplateClient(
  templateId: string,
  request: UpdateTemplateRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/templates/${templateId}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

// ============================================================================
// LETTERHEADS
// ============================================================================

export interface ListLetterheadsParams {
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export async function listLetterheadsClient(
  params: ListLetterheadsParams = {},
): Promise<ListLetterheadsResult> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.activeOnly) queryParams.set("activeOnly", "true");
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/automation/letterheads${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
    },
  });

  await throwIfErrorResponse(res);
  const data = (await res.json()) as {
    letterheads?: Letterhead[];
    Letterheads?: Letterhead[];
    total?: number;
    Total?: number;
    page?: number;
    Page?: number;
    pageSize?: number;
    PageSize?: number;
    totalPages?: number;
    TotalPages?: number;
  };
  return {
    letterheads: data.letterheads ?? data.Letterheads ?? [],
    total: data.total ?? data.Total ?? 0,
    page: data.page ?? data.Page ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? 20,
    totalPages: data.totalPages ?? data.TotalPages ?? 0,
  };
}

export async function getLetterheadByIdClient(letterheadId: string): Promise<Letterhead> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/letterheads/${letterheadId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  await throwIfErrorResponse(res);

  return (await res.json()) as Letterhead;
}

export async function createLetterheadClient(
  request: CreateLetterheadRequest,
): Promise<{ letterheadId: string }> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/letterheads`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  await throwIfErrorResponse(res);

  const letterheadId = await res.text();
  return { letterheadId: letterheadId.replace(/"/g, "") };
}

export async function updateLetterheadClient(
  letterheadId: string,
  request: UpdateLetterheadRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/letterheads/${letterheadId}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

export async function deleteReminderClient(reminderId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/reminders/${reminderId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

export async function deleteDraftClient(draftId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/drafts/${draftId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

export async function deleteTemplateClient(templateId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/templates/${templateId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}

export async function deleteLetterheadClient(letterheadId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/automation/letterheads/${letterheadId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }
  await throwIfErrorResponse(res);
}
