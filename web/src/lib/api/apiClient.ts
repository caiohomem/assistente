import { getApiBaseUrl, getBffSession, BffSession } from "@/lib/bff";

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
  private async getSession(cookieHeader?: string): Promise<BffSession> {
    return await getBffSession(cookieHeader ? { cookieHeader } : undefined);
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
      requiresCsrf = true,
      cookieHeader,
    } = options;

    // Obter sessão e CSRF token se necessário
    let csrfToken: string | undefined;
    if (requiresAuth || requiresCsrf) {
      const session = await this.getSession(cookieHeader);
      if (requiresAuth && !session.authenticated) {
        throw new Error("Não autenticado");
      }
      if (requiresCsrf && session.csrfToken) {
        csrfToken = session.csrfToken;
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
    const isJson = contentType.includes("application/json");
    const data = isJson ? await res.json() : undefined;

    if (!res.ok) {
      // Tratar 401 (não autorizado) - redirecionar para login no cliente
      if (res.status === 401 && typeof window !== "undefined") {
        const currentPath = window.location.pathname;
        const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
        window.location.href = loginUrl;
        return undefined as TResponse; // Não lançar erro para evitar loop
      }

      const message =
        (data &&
          typeof data === "object" &&
          "message" in data &&
          String((data as any).message)) ||
        `Request failed: ${res.status}`;
      throw new Error(message);
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

