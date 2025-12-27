import fetch from "node-fetch";
import { ApiError } from "./types";
import { ApiClientConfig } from "./config";

export class HttpClient {
  private baseUrl: string;
  private timeout: number;
  private cookies: Map<string, string> = new Map();

  constructor(config: ApiClientConfig) {
    this.baseUrl = config.baseUrl.replace(/\/$/, "");
    this.timeout = config.timeout || 30000;
  }

  /**
   * Define cookies para serem enviados nas requisições
   */
  setCookies(cookies: Map<string, string>): void {
    this.cookies = cookies;
  }

  /**
   * Obtém cookies atuais
   */
  getCookies(): Map<string, string> {
    return new Map(this.cookies);
  }

  /**
   * Faz uma requisição HTTP
   */
  async request<T>(
    method: string,
    path: string,
    options: {
      body?: any;
      headers?: Record<string, string>;
      params?: Record<string, any>;
    } = {}
  ): Promise<T> {
    const url = new URL(path.startsWith("http") ? path : `${this.baseUrl}${path}`);
    
    // Adicionar query parameters
    if (options.params) {
      Object.entries(options.params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          url.searchParams.append(key, String(value));
        }
      });
    }

    const headers: Record<string, string> = {
      ...options.headers,
    };

    // Adicionar Content-Type apenas se não for FormData
    const isFormData = options.body && 
      options.body.constructor && 
      (options.body.constructor.name === "FormData" || 
       typeof (options.body as any).getBoundary === "function");
    
    if (!isFormData) {
      headers["Content-Type"] = "application/json";
    }

    // Adicionar cookies
    const cookieString = Array.from(this.cookies.entries())
      .map(([key, value]) => `${key}=${value}`)
      .join("; ");
    
    if (cookieString) {
      headers["Cookie"] = cookieString;
    }

    const fetchOptions: any = {
      method,
      headers,
      timeout: this.timeout,
    };

    if (options.body) {
      // Verificar se é FormData (pode ser do pacote form-data ou do browser)
      if (isFormData) {
        fetchOptions.body = options.body as any;
      } else {
        fetchOptions.body = JSON.stringify(options.body);
      }
    }

    try {
      const response = await fetch(url.toString(), fetchOptions);

      // Extrair cookies da resposta
      const setCookieHeaders = response.headers.raw()["set-cookie"] || [];
      setCookieHeaders.forEach((cookieHeader) => {
        const [cookiePart] = cookieHeader.split(";");
        const [key, value] = cookiePart.split("=");
        if (key && value) {
          this.cookies.set(key.trim(), value.trim());
        }
      });

      const contentType = response.headers.get("content-type") || "";
      const isJson = contentType.includes("application/json");
      const isBinary = contentType.includes("application/octet-stream") || 
                       contentType.includes("audio/") ||
                       contentType.includes("image/") ||
                       contentType.includes("video/");

      let data: any;
      if (isJson) {
        data = await response.json();
      } else if (isBinary) {
        const arrayBuffer = await response.arrayBuffer();
        data = Buffer.from(arrayBuffer);
      } else {
        data = await response.text();
      }

      if (!response.ok) {
        const error: ApiError = {
          message: data.message || data.error || `HTTP ${response.status}`,
          error: data.error,
          statusCode: response.status,
        };
        throw error;
      }

      return data as T;
    } catch (error: any) {
      if (error.statusCode) {
        throw error;
      }
      throw {
        message: error.message || "Erro na requisição",
        statusCode: 0,
      } as ApiError;
    }
  }

  /**
   * GET request
   */
  async get<T>(path: string, params?: Record<string, any>, headers?: Record<string, string>): Promise<T> {
    return this.request<T>("GET", path, { params, headers });
  }

  /**
   * POST request
   */
  async post<T>(path: string, body?: any, headers?: Record<string, string>): Promise<T> {
    return this.request<T>("POST", path, { body, headers });
  }

  /**
   * PUT request
   */
  async put<T>(path: string, body?: any, headers?: Record<string, string>): Promise<T> {
    return this.request<T>("PUT", path, { body, headers });
  }

  /**
   * DELETE request
   */
  async delete<T>(path: string, headers?: Record<string, string>): Promise<T> {
    return this.request<T>("DELETE", path, { headers });
  }
}

