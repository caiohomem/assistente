import { buildApiError } from "./api/types";

type ErrorWithMeta = Error & {
  status?: number;
  url?: string;
};

export type BffSession = {
  authenticated: boolean;
  csrfToken: string;
  accessToken?: string | null;
  expiresAtUnix?: number | null;
  user?: {
    sub?: string | null;
    email?: string | null;
    name?: string | null;
    givenName?: string | null;
    familyName?: string | null;
  } | null;
};

export function getApiBaseUrl(): string {
  return (process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5239").replace(/\/+$/, "");
}

export function getN8nWebhookBaseUrl(): string {
  return (process.env.NEXT_PUBLIC_N8N_WEBHOOK_BASE_URL || "").trim().replace(/\/+$/, "");
}

export function redirectToLogin(returnUrl?: string): void {
  if (typeof window === "undefined") {
    return;
  }
  const currentPath = returnUrl ?? window.location.pathname + window.location.search;
  window.location.href = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
}

const parseJsonResponse = async <T>(res: Response): Promise<T | undefined> => {
  try {
    return (await res.json()) as T;
  } catch {
    return undefined;
  }
};

function getFrontendBaseUrl(): string {
  return (
    process.env.NEXT_PUBLIC_FRONTEND_BASE_URL ||
    process.env.NEXT_PUBLIC_APP_URL ||
    "http://127.0.0.1:3000"
  ).replace(/\/+$/, "");
}

export async function getBffSession(_params?: { cookieHeader?: string }): Promise<BffSession> {
  const isServer = typeof window === "undefined";
  const url = isServer ? `${getFrontendBaseUrl()}/api/auth/session` : "/api/auth/session";
  const res = await fetch(url, {
    method: "GET",
    cache: "no-store",
    credentials: "include",
    headers: _params?.cookieHeader ? { Cookie: _params.cookieHeader } : undefined,
  });

  if (!res.ok) {
    throw new Error(`Failed to fetch Clerk session: ${res.status} ${res.statusText}`);
  }

  return (await res.json()) as BffSession;
}

async function buildAuthHeaders(additionalHeaders?: HeadersInit): Promise<HeadersInit> {
  const session = await getBffSession();
  const headers = new Headers(additionalHeaders);

  if (session.accessToken) {
    headers.set("Authorization", `Bearer ${session.accessToken}`);
  }

  if (session.user?.email) {
    headers.set("X-User-Email", session.user.email);
  }

  if (session.user?.name) {
    headers.set("X-User-Name", session.user.name);
  }

  return headers;
}

export async function bffPostJson<TResponse>(
  path: string,
  body: unknown,
  _csrfToken: string,
  _cookieHeader?: string,
): Promise<TResponse> {
  const apiBase = getApiBaseUrl();
  const headers = await buildAuthHeaders({
    "Content-Type": "application/json",
  });

  const res = await fetch(`${apiBase}${path}`, {
    method: "POST",
    credentials: "include",
    headers,
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    const error = (await buildApiError(res)) as ErrorWithMeta;
    error.status = res.status;
    error.url = `${apiBase}${path}`;
    throw error;
  }

  const data = await parseJsonResponse<TResponse>(res);
  return data ?? ({} as TResponse);
}

export async function bffPostNoContent(
  path: string,
  _csrfToken: string,
  _cookieHeader?: string,
): Promise<void> {
  const apiBase = getApiBaseUrl();
  const headers = await buildAuthHeaders();

  const res = await fetch(`${apiBase}${path}`, {
    method: "POST",
    credentials: "include",
    headers,
  });

  if (!res.ok) {
    const error = (await buildApiError(res)) as ErrorWithMeta;
    error.status = res.status;
    error.url = `${apiBase}${path}`;
    throw error;
  }
}

export async function bffGetJson<TResponse>(
  path: string,
  _csrfToken?: string,
  _cookieHeader?: string,
): Promise<TResponse> {
  const apiBase = getApiBaseUrl();
  const headers = await buildAuthHeaders({
    "Content-Type": "application/json",
  });

  const url = `${apiBase}${path}`;
  const res = await fetch(url, {
    method: "GET",
    credentials: "include",
    headers,
  });

  if (!res.ok) {
    const error = (await buildApiError(res)) as ErrorWithMeta;
    error.status = res.status;
    error.url = url;
    throw error;
  }

  const data = await parseJsonResponse<TResponse>(res);
  return data ?? ({} as TResponse);
}

export async function bffPutJson<TResponse>(
  path: string,
  body: unknown,
  _csrfToken: string,
  _cookieHeader?: string,
): Promise<TResponse> {
  const apiBase = getApiBaseUrl();
  const headers = await buildAuthHeaders({
    "Content-Type": "application/json",
  });

  const res = await fetch(`${apiBase}${path}`, {
    method: "PUT",
    credentials: "include",
    headers,
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    const error = (await buildApiError(res)) as ErrorWithMeta;
    error.status = res.status;
    error.url = `${apiBase}${path}`;
    throw error;
  }

  const data = await parseJsonResponse<TResponse>(res);
  return data ?? ({} as TResponse);
}

export async function bffDelete(
  path: string,
  _csrfToken: string,
  _cookieHeader?: string,
): Promise<void> {
  const apiBase = getApiBaseUrl();
  const headers = await buildAuthHeaders();

  const res = await fetch(`${apiBase}${path}`, {
    method: "DELETE",
    credentials: "include",
    headers,
  });

  if (!res.ok) {
    const error = (await buildApiError(res)) as ErrorWithMeta;
    error.status = res.status;
    error.url = `${apiBase}${path}`;
    throw error;
  }
}
