import { HttpClient } from "./http-client";

export interface AgentConfiguration {
  configurationId: string;
  contextPrompt: string;
  transcriptionPrompt?: string;
  createdAt: string;
  updatedAt: string;
}

export class AgentConfigurationService {
  constructor(private http: HttpClient) {}

  /**
   * Obtém a configuração atual do agente
   */
  async getCurrent(): Promise<AgentConfiguration> {
    return this.http.get<AgentConfiguration>("/api/agent-configuration");
  }

  /**
   * Atualiza ou cria a configuração do agente
   */
  async updateOrCreate(data: { contextPrompt: string; transcriptionPrompt?: string }): Promise<AgentConfiguration> {
    return this.http.put<AgentConfiguration>("/api/agent-configuration", data);
  }
}

