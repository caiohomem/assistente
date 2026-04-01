import { getApiBaseUrl, getBffSession, BffSession } from "@/lib/bff";
import { ApiError, extractApiErrorMessage } from "./types";

/**
 * Cliente HTTP para fazer requisições diretas à API (sem proxy do Next.js).
 *
 * Todas as requisições são feitas diretamente para a URL configurada em
 * NEXT_PUBLIC_API_BASE_URL, facilitando o desenvolvimento e debugging.
 */
export interface ApiRequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE" | "PATCH";
  body?: unknown;
  headers?: Record<string, string>;
  requiresAuth?: boolean;
  requiresCsrf?: boolean;
  cookieHeader?: string;
}

export class ApiClient {
  private baseUrl: string;

  constructor() {
    this.baseUrl = getApiBaseUrl();
  }

  /**
   * Obtém o token CSRF e sessão.
   * Funciona tanto no servidor (com cookieHeader) quanto no cliente.
   */
  private async getSession(_cookieHeader?: string): Promise<BffSession> {
    return await getBffSession(_cookieHeader ? { cookieHeader: _cookieHeader } : undefined);
  }

  /**
   * Executa uma requisição HTTP.
   */
  async request<TResponse>(
    path: string,
    options: ApiRequestOptions = {},
  ): Promise<TResponse> {
    const {
      method = "GET",
      body,
      headers = {},
      requiresAuth = true,
      requiresCsrf = false,
      cookieHeader,
    } = options;

    // Obter sessão e CSRF token se necessário
    let csrfToken: string | undefined;
    if (requiresAuth || requiresCsrf) {
      const session = await this.getSession(cookieHeader);
      if (requiresAuth && (!session.authenticated || !session.accessToken)) {
        throw new Error("Não autenticado");
      }
      csrfToken = session.csrfToken;

      if (session.accessToken) {
        headers.Authorization = `Bearer ${session.accessToken}`;
      }

      if (session.user?.email) {
        headers["X-User-Email"] = session.user.email;
      }

      if (session.user?.name) {
        headers["X-User-Name"] = session.user.name;
      }
    }

    // Preparar headers
    const requestHeaders: HeadersInit = {
      ...headers,
    };

    // Adicionar CSRF token se necessário
    if (requiresCsrf && csrfToken) {
      requestHeaders["X-CSRF-TOKEN"] = csrfToken;
    }

    // Preparar body
    let requestBody: BodyInit | undefined;
    if (body) {
      if (body instanceof FormData) {
        // Para FormData, não definir Content-Type (deixar o browser definir)
        requestBody = body;
      } else {
        // Para JSON, definir Content-Type e stringify
        requestHeaders["Content-Type"] = "application/json";
        requestBody = JSON.stringify(body);
      }
    }

    // Fazer requisição
    const url = path.startsWith("http") ? path : `${this.baseUrl}${path}`;
    const res = await fetch(url, {
      method,
      credentials: "include",
      headers: requestHeaders,
      body: requestBody,
      cache: method === "GET" ? "no-store" : undefined,
    });

    // Processar resposta
    const contentType = res.headers.get("content-type") ?? "";
    const isJson = contentType.includes("application/json") || contentType.includes("application/problem+json");
    const data = isJson ? await res.json() : undefined;

    if (!res.ok) {
      if (process.env.NODE_ENV === "development") {
        console.log("[ApiClient] Error response:", { status: res.status, contentType, data });
      }

      const message = extractApiErrorMessage(data) ?? `Request failed: ${res.status}`;
      throw new ApiError(message, res.status, data);
    }

    // Se não há conteúdo, retornar void
    if (res.status === 204 || !isJson) {
      return undefined as TResponse;
    }

    return data as TResponse;
  }

  /**
   * GET request
   */
  async get<TResponse>(
    path: string,
    options?: Omit<ApiRequestOptions, "method" | "body">,
  ): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: "GET" });
  }

  /**
   * POST request
   */
  async post<TResponse>(
    path: string,
    body?: unknown,
    options?: Omit<ApiRequestOptions, "method" | "body">,
  ): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: "POST", body });
  }

  /**
   * PUT request
   */
  async put<TResponse>(
    path: string,
    body?: unknown,
    options?: Omit<ApiRequestOptions, "method" | "body">,
  ): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: "PUT", body });
  }

  /**
   * DELETE request
   */
  async delete(
    path: string,
    options?: Omit<ApiRequestOptions, "method" | "body">,
  ): Promise<void> {
    return this.request<void>(path, { ...options, method: "DELETE" });
  }

  /**
   * PATCH request
   */
  async patch<TResponse>(
    path: string,
    body?: unknown,
    options?: Omit<ApiRequestOptions, "method" | "body">,
  ): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: "PATCH", body });
  }
}

// Instância singleton para uso em componentes client-side
export const apiClient = new ApiClient();

// Função helper para uso em Server Components (com cookies)
export async function getApiClientForServer(cookieHeader?: string) {
  const client = new ApiClient();
  return {
    get: <TResponse>(path: string, options?: Omit<ApiRequestOptions, "method" | "body">) =>
      client.get<TResponse>(path, { ...options, cookieHeader }),
    post: <TResponse>(
      path: string,
      body?: unknown,
      options?: Omit<ApiRequestOptions, "method" | "body">,
    ) => client.post<TResponse>(path, body, { ...options, cookieHeader }),
    put: <TResponse>(
      path: string,
      body?: unknown,
      options?: Omit<ApiRequestOptions, "method" | "body">,
    ) => client.put<TResponse>(path, body, { ...options, cookieHeader }),
    delete: (path: string, options?: Omit<ApiRequestOptions, "method" | "body">) =>
      client.delete(path, { ...options, cookieHeader }),
    patch: <TResponse>(
      path: string,
      body?: unknown,
      options?: Omit<ApiRequestOptions, "method" | "body">,
    ) => client.patch<TResponse>(path, body, { ...options, cookieHeader }),
  };
}
