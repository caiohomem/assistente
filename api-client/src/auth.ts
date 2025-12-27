import { HttpClient } from "./http-client";
import { SessionInfo } from "./types";
import { ApiClientConfig } from "./config";

export class AuthService {
  private http: HttpClient;
  private frontendUrl: string;

  constructor(http: HttpClient, config: ApiClientConfig) {
    this.http = http;
    this.frontendUrl = (config.frontendUrl || "").replace(/\/$/, "");
  }

  /**
   * Obtém informações da sessão atual
   */
  async getSession(): Promise<SessionInfo> {
    return this.http.get<SessionInfo>("/auth/session");
  }

  /**
   * Verifica se o usuário está autenticado
   */
  async isAuthenticated(): Promise<boolean> {
    try {
      const session = await this.getSession();
      return session.authenticated;
    } catch {
      return false;
    }
  }

  /**
   * Retorna a URL de login (para redirecionamento)
   */
  getLoginUrl(returnUrl?: string): string {
    const params = new URLSearchParams();
    if (returnUrl) {
      params.append("returnUrl", returnUrl);
    }
    const query = params.toString();
    return `${this.http["baseUrl"]}/auth/login${query ? `?${query}` : ""}`;
  }

  /**
   * Retorna a URL de registro (para redirecionamento)
   */
  getRegisterUrl(returnUrl?: string): string {
    const params = new URLSearchParams();
    if (returnUrl) {
      params.append("returnUrl", returnUrl);
    }
    const query = params.toString();
    return `${this.http["baseUrl"]}/auth/register${query ? `?${query}` : ""}`;
  }

  /**
   * Faz logout
   */
  async logout(): Promise<void> {
    try {
      await this.http.post("/auth/logout");
    } catch (error) {
      // Ignorar erros no logout
      console.warn("Erro ao fazer logout:", error);
    }
  }

  /**
   * Registra um novo usuário (requer email e senha)
   */
  async register(email: string, password: string, firstName: string, lastName?: string): Promise<any> {
    return this.http.post("/auth/register", {
      email,
      password,
      firstName,
      lastName,
    });
  }
}

