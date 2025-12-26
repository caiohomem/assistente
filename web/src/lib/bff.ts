export type BffSession = {
  authenticated: boolean;
  csrfToken: string;
  expiresAtUnix?: number | null;
  user?: {
    sub?: string | null;
    email?: string | null;
    name?: string | null;
    givenName?: string | null;
    familyName?: string | null;
  } | null;
};

/**
 * Retorna a URL base da API para chamadas diretas (sem proxy).
 * 
 * IMPORTANTE: As chamadas são feitas diretamente do cliente/servidor para a API,
 * sem passar por proxy do Next.js. Isso facilita o desenvolvimento e debugging.
 * 
 * Configure NEXT_PUBLIC_API_BASE_URL no .env.local para apontar para sua API.
 * Exemplo: NEXT_PUBLIC_API_BASE_URL=http://localhost:5239
 * 
 * Se necessário, pode-se adicionar proxy no next.config.ts no futuro.
 */
export function getApiBaseUrl(): string {
  return (process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5239").replace(
    /\/+$/,
    "",
  );
}

export async function getBffSession(params?: {
  cookieHeader?: string;
}): Promise<BffSession> {
  const apiBase = getApiBaseUrl();
  const url = `${apiBase}/auth/session`;
  
  // Log para debug (apenas no servidor)
  if (typeof window === "undefined") {
    console.log(`[Server] Fetching BFF session from: ${url}`);
    console.log(`[Server] API Base URL: ${apiBase}`);
    console.log(`[Server] Has cookie header: ${!!params?.cookieHeader}`);
  }
  
  try {
    // Criar AbortController para timeout (compatível com Node.js 17+)
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 10000); // 10 segundos
    
    try {
      const res = await fetch(url, {
        method: "GET",
        cache: "no-store",
        credentials: "include",
        headers: params?.cookieHeader ? { cookie: params.cookieHeader } : undefined,
        signal: controller.signal,
      });
      
      clearTimeout(timeoutId);

      if (!res.ok) {
        // Log detalhado para debug
        const isClient = typeof window !== "undefined";
        if (isClient) {
          console.error(`[Client] Failed to fetch /auth/session: ${res.status} ${res.statusText}`);
          console.error(`[Client] URL: ${url}`);
          console.error(`[Client] Cookies available:`, document.cookie);
        } else {
          console.error(`[Server] Failed to fetch /auth/session: ${res.status} ${res.statusText}`);
          console.error(`[Server] URL: ${url}`);
          console.error(`[Server] API Base URL: ${apiBase}`);
          console.error(`[Server] Has cookie header: ${!!params?.cookieHeader}`);
        }
        
        // Se for 401, retornar sessão não autenticada ao invés de lançar erro
        if (res.status === 401) {
          return {
            authenticated: false,
            csrfToken: "",
            user: null,
            expiresAtUnix: null
          } as BffSession;
        }
        
        throw new Error(`Failed to fetch /auth/session: ${res.status} ${res.statusText}`);
      }

      const session = (await res.json()) as BffSession;
      
      // Log para debug no cliente
      if (typeof window !== "undefined") {
        console.log(`[Client] Session check:`, {
          authenticated: session.authenticated,
          hasUser: !!session.user,
          userEmail: session.user?.email
        });
      }
      
      return session;
    } finally {
      clearTimeout(timeoutId);
    }
  } catch (error) {
    // Log detalhado para debug (apenas no servidor)
    if (typeof window === "undefined") {
      console.error(`[Server] Fetch error for /auth/session`);
      console.error(`[Server] URL: ${url}`);
      console.error(`[Server] API Base URL: ${apiBase}`);
      console.error(`[Server] Environment: NEXT_PUBLIC_API_BASE_URL=${process.env.NEXT_PUBLIC_API_BASE_URL || "not set"}`);
      console.error(`[Server] Error:`, error);
      
      // Verificar se é um erro de conexão
      if (error instanceof Error) {
        const errorMessage = error.message.toLowerCase();
        if (errorMessage.includes("fetch failed") || 
            errorMessage.includes("econnrefused") ||
            errorMessage.includes("connect econnrefused") ||
            errorMessage.includes("networkerror") ||
            errorMessage.includes("failed to fetch")) {
          console.error(`[Server] Connection error detected.`);
          console.error(`[Server] Please verify:`);
          console.error(`[Server]   1. Is the API running at ${apiBase}?`);
          console.error(`[Server]   2. Check if NEXT_PUBLIC_API_BASE_URL is set correctly in .env.local`);
          console.error(`[Server]   3. Try accessing ${url} directly in your browser`);
        }
        
        // Re-throw com mensagem mais clara
        if (errorMessage.includes("aborted") || errorMessage.includes("timeout")) {
          throw new Error(`Timeout connecting to API at ${apiBase}. Is the API running?`);
        }
      }
    }
    throw error;
  }
}

export async function bffPostJson<TResponse>(
  path: string,
  body: unknown,
  csrfToken: string,
  cookieHeader?: string,
): Promise<TResponse> {
  const apiBase = getApiBaseUrl();
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    "X-CSRF-TOKEN": csrfToken,
  };
  
  // Para chamadas do servidor, passar cookies manualmente
  if (cookieHeader) {
    headers["Cookie"] = cookieHeader;
  }

  const res = await fetch(`${apiBase}${path}`, {
    method: "POST",
    credentials: "include",
    headers,
    body: JSON.stringify(body),
  });

  const contentType = res.headers.get("content-type") ?? "";
  const maybeJson = contentType.includes("application/json");
  const data = maybeJson ? await res.json() : undefined;

  if (!res.ok) {
    // Tratar 401 (Não autorizado) - redirecionar para login no cliente
    if (res.status === 401 && typeof window !== "undefined") {
      const currentPath = window.location.pathname;
      const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
      window.location.href = loginUrl;
      // Retornar um valor padrão para evitar erro, mas nunca será usado pois redireciona
      return {} as TResponse;
    }

    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return data as TResponse;
}

export async function bffPostNoContent(
  path: string,
  csrfToken: string,
  cookieHeader?: string,
): Promise<void> {
  const apiBase = getApiBaseUrl();
  const headers: HeadersInit = {
    "X-CSRF-TOKEN": csrfToken,
  };
  
  // Para chamadas do servidor, passar cookies manualmente
  if (cookieHeader) {
    headers["Cookie"] = cookieHeader;
  }

  const res = await fetch(`${apiBase}${path}`, {
    method: "POST",
    credentials: "include",
    headers,
  });

  if (!res.ok) {
    // Tratar 401 (Não autorizado) - redirecionar para login no cliente
    if (res.status === 401 && typeof window !== "undefined") {
      const currentPath = window.location.pathname;
      const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
      window.location.href = loginUrl;
      return; // Não lançar erro para evitar loop
    }

    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

export async function bffGetJson<TResponse>(
  path: string,
  csrfToken?: string,
  cookieHeader?: string,
): Promise<TResponse> {
  const apiBase = getApiBaseUrl();
  const headers: HeadersInit = {
    "Content-Type": "application/json",
  };
  
  if (csrfToken) {
    headers["X-CSRF-TOKEN"] = csrfToken;
  }
  
  // Para chamadas do servidor, passar cookies manualmente
  if (cookieHeader) {
    headers["Cookie"] = cookieHeader;
  }

  const url = `${apiBase}${path}`;
  
  // Log para debug (apenas no servidor)
  if (typeof window === "undefined") {
    console.log(`[Server] GET ${url}`);
    console.log(`[Server] Headers:`, {
      hasCsrfToken: !!csrfToken,
      hasCookieHeader: !!cookieHeader,
      cookieCount: cookieHeader ? cookieHeader.split(";").length : 0,
    });
  }

  const res = await fetch(url, {
    method: "GET",
    credentials: "include",
    headers,
  });

  const contentType = res.headers.get("content-type") ?? "";
  const maybeJson = contentType.includes("application/json");
  const data = maybeJson ? await res.json() : undefined;

  if (!res.ok) {
    // Tratar 401 (Não autorizado) - redirecionar para login no cliente
    if (res.status === 401 && typeof window !== "undefined") {
      const currentPath = window.location.pathname;
      const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
      window.location.href = loginUrl;
      // Retornar um valor padrão para evitar erro, mas nunca será usado pois redireciona
      return {} as TResponse;
    }

    // Log detalhado para debug (apenas no servidor)
    if (typeof window === "undefined") {
      console.error(`[Server] Request failed: ${res.status} ${res.statusText}`);
      console.error(`[Server] URL: ${url}`);
      console.error(`[Server] Response:`, data);
      console.error(`[Server] Response headers:`, Object.fromEntries(res.headers.entries()));
    }
    
    // 404 é um caso esperado para endpoints opcionais (como /api/plans)
    // Não logar como erro crítico
    if (res.status === 404) {
      const error = new Error(`Endpoint not found: ${path}`);
      (error as any).status = 404;
      throw error;
    }
    
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    const error = new Error(message);
    (error as any).status = res.status;
    (error as any).url = url;
    throw error;
  }

  return data as TResponse;
}

export async function bffPutJson<TResponse>(
  path: string,
  body: unknown,
  csrfToken: string,
  cookieHeader?: string,
): Promise<TResponse> {
  const apiBase = getApiBaseUrl();
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    "X-CSRF-TOKEN": csrfToken,
  };
  
  // Para chamadas do servidor, passar cookies manualmente
  if (cookieHeader) {
    headers["Cookie"] = cookieHeader;
  }

  const res = await fetch(`${apiBase}${path}`, {
    method: "PUT",
    credentials: "include",
    headers,
    body: JSON.stringify(body),
  });

  const contentType = res.headers.get("content-type") ?? "";
  const maybeJson = contentType.includes("application/json");
  const data = maybeJson ? await res.json() : undefined;

  if (!res.ok) {
    // Tratar 401 (Não autorizado) - redirecionar para login no cliente
    if (res.status === 401 && typeof window !== "undefined") {
      const currentPath = window.location.pathname;
      const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
      window.location.href = loginUrl;
      // Retornar um valor padrão para evitar erro, mas nunca será usado pois redireciona
      return {} as TResponse;
    }

    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return data as TResponse;
}

export async function bffDelete(
  path: string,
  csrfToken: string,
  cookieHeader?: string,
): Promise<void> {
  const apiBase = getApiBaseUrl();
  const headers: HeadersInit = {
    "X-CSRF-TOKEN": csrfToken,
  };
  
  // Para chamadas do servidor, passar cookies manualmente
  if (cookieHeader) {
    headers["Cookie"] = cookieHeader;
  }

  const res = await fetch(`${apiBase}${path}`, {
    method: "DELETE",
    credentials: "include",
    headers,
  });

  if (!res.ok) {
    // Tratar 401 (Não autorizado) - redirecionar para login no cliente
    if (res.status === 401 && typeof window !== "undefined") {
      const currentPath = window.location.pathname;
      const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
      window.location.href = loginUrl;
      return; // Não lançar erro para evitar loop
    }

    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

