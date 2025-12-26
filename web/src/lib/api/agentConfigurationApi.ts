"use client";

import { getApiBaseUrl } from "@/lib/bff";

export interface AgentConfiguration {
  configurationId: string;
  contextPrompt: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateAgentConfigurationRequest {
  contextPrompt: string;
}

/**
 * Obtém a configuração atual do agente.
 */
export async function getAgentConfiguration(): Promise<AgentConfiguration> {
  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/agent-configuration`;
  
  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!res.ok) {
    if (res.status === 404) {
      throw new Error("Configuração não encontrada");
    }
    throw new Error(`Erro ao obter configuração: ${res.statusText}`);
  }

  return res.json();
}

/**
 * Atualiza ou cria a configuração do agente.
 */
export async function updateAgentConfiguration(
  request: UpdateAgentConfigurationRequest
): Promise<AgentConfiguration> {
  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/agent-configuration`;
  
  const res = await fetch(path, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!res.ok) {
    throw new Error(`Erro ao atualizar configuração: ${res.statusText}`);
  }

  return res.json();
}


