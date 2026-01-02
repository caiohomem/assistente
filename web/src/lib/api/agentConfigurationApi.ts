"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";

export interface AgentConfiguration {
  configurationId: string;
  ocrPrompt: string;
  transcriptionPrompt?: string;
  workflowPrompt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateAgentConfigurationRequest {
  ocrPrompt: string;
  transcriptionPrompt?: string;
  workflowPrompt?: string;
}

/**
 * Obtém a configuração atual do agente.
 */
export async function getAgentConfiguration(): Promise<AgentConfiguration> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/agent-configuration`;

  const headers: HeadersInit = {
    "Content-Type": "application/json",
  };

  if (session.csrfToken) {
    headers["X-CSRF-TOKEN"] = session.csrfToken;
  }
  
  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers,
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
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/agent-configuration`;
  
  const res = await fetch(path, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(request),
  });

  if (!res.ok) {
    throw new Error(`Erro ao atualizar configuração: ${res.statusText}`);
  }

  return res.json();
}



