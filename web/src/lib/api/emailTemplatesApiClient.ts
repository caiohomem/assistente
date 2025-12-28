"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import type {
  EmailTemplate,
  CreateEmailTemplateRequest,
  UpdateEmailTemplateRequest,
  ListEmailTemplatesResult,
  ListEmailTemplatesParams,
  EmailTemplateType,
} from "@/lib/types/emailTemplates";

// Re-export types for convenience
export type { EmailTemplate, ListEmailTemplatesResult };

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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  const data = await res.json();
  return {
    templates: data.templates || data.Templates || [],
    total: data.total || data.Total || 0,
    page: data.page || data.Page || 1,
    pageSize: data.pageSize || data.PageSize || 20,
    totalPages: data.totalPages || data.TotalPages || 0,
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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return await res.json() as EmailTemplate;
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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
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

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

