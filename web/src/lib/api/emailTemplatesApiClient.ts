"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import { throwIfErrorResponse } from "./types";
import type {
  EmailTemplate,
  CreateEmailTemplateRequest,
  UpdateEmailTemplateRequest,
  ListEmailTemplatesResult,
  ListEmailTemplatesParams,
} from "@/lib/types/emailTemplates";

// Re-export types for convenience
export type { EmailTemplate, ListEmailTemplatesResult };

interface ListEmailTemplatesResponse {
  templates?: EmailTemplate[]
  Templates?: EmailTemplate[]
  total?: number
  Total?: number
  page?: number
  Page?: number
  pageSize?: number
  PageSize?: number
  totalPages?: number
  TotalPages?: number
}

export async function listEmailTemplatesClient(
  params: ListEmailTemplatesParams = {},
): Promise<ListEmailTemplatesResult> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.templateType !== undefined) queryParams.set("templateType", params.templateType.toString());
  if (params.activeOnly) queryParams.set("activeOnly", "true");
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/email-templates${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;

  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
    },
  });

  await throwIfErrorResponse(res);

  const data = (await res.json()) as ListEmailTemplatesResponse;
  return {
    templates: data.templates ?? data.Templates ?? [],
    total: data.total ?? data.Total ?? 0,
    page: data.page ?? data.Page ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? 20,
    totalPages: data.totalPages ?? data.TotalPages ?? 0,
  };
}

export async function getEmailTemplateByIdClient(emailTemplateId: string): Promise<EmailTemplate> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/email-templates/${emailTemplateId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  await throwIfErrorResponse(res);

  return (await res.json()) as EmailTemplate;
}

export async function createEmailTemplateClient(
  request: CreateEmailTemplateRequest,
): Promise<{ id: string }> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/email-templates`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  await throwIfErrorResponse(res);

  const id = await res.text();
  return { id: id.replace(/"/g, "") };
}

export async function updateEmailTemplateClient(
  emailTemplateId: string,
  request: UpdateEmailTemplateRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/email-templates/${emailTemplateId}`, {
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

export async function activateEmailTemplateClient(emailTemplateId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/email-templates/${emailTemplateId}/activate`, {
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

export async function deactivateEmailTemplateClient(emailTemplateId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/email-templates/${emailTemplateId}/deactivate`, {
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

export async function deleteEmailTemplateClient(emailTemplateId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/email-templates/${emailTemplateId}`, {
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
