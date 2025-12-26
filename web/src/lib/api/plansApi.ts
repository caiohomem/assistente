import { bffGetJson } from "../bff";

import type { Plan } from "../types/plan";

/**
 * Lista todos os planos disponíveis.
 * Retorna array vazio se o endpoint não existir (fallback para dados hardcoded).
 */
export async function listPlans(): Promise<Plan[]> {
  try {
    // Tentar buscar do backend (endpoint pode não existir ainda)
    return await bffGetJson<Plan[]>("/api/plans");
  } catch (error: any) {
    // Se o endpoint não existir (404), retornar array vazio silenciosamente
    // A landing page usará dados hardcoded como fallback
    if (error?.status === 404) {
      // Endpoint não implementado ainda - comportamento esperado
      return [];
    }
    // Para outros erros, logar como warning
    console.warn("Erro ao buscar planos do backend, usando dados hardcoded:", error);
    return [];
  }
}

/**
 * Obtém um plano por ID.
 */
export async function getPlanById(planId: string): Promise<Plan | null> {
  try {
    return await bffGetJson<Plan>(`/api/plans/${planId}`);
  } catch (error: any) {
    // Se o endpoint não existir (404), retornar null silenciosamente
    if (error?.status === 404) {
      return null;
    }
    // Para outros erros, logar como warning
    console.warn(`Erro ao buscar plano ${planId}:`, error);
    return null;
  }
}

