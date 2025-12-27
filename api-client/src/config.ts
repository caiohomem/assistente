// Configuração do cliente API

export interface ApiClientConfig {
  baseUrl: string;
  frontendUrl?: string;
  timeout?: number;
}

export const defaultConfig: ApiClientConfig = {
  baseUrl: process.env.API_BASE_URL || "https://api.assistente.live",
  frontendUrl: process.env.FRONTEND_URL || "https://web.assistente.live",
  timeout: 30000 // 30 segundos
};

