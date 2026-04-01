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

async function getAuthorizedHeaders(contentType = true): Promise<HeadersInit> {
  const session = await getBffSession();
  if (!session.authenticated || !session.accessToken) {
    throw new Error("Não autenticado");
  }

  return {
    ...(contentType ? { "Content-Type": "application/json" } : {}),
    Authorization: `Bearer ${session.accessToken}`,
    ...(session.user?.email ? { "X-User-Email": session.user.email } : {}),
    ...(session.user?.name ? { "X-User-Name": session.user.name } : {}),
  };
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
    headers: await getAuthorizedHeaders(),
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
    headers: await getAuthorizedHeaders(),
    body: JSON.stringify(request),
  });

  if (!res.ok) {
    throw new Error(`Erro ao atualizar configuração: ${res.statusText}`);
  }

  return res.json();
}


