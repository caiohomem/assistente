import { bffGetJson } from "../bff";

import type { Plan } from "../types/plan";

const isNotFoundError = (error: unknown): boolean => {
  if (error instanceof Error && "status" in error) {
    const status = (error as { status?: number }).status;
    return status === 404;
  }
  return false;
};

/**
 * Lista todos os planos disponíveis.
 * Retorna array vazio se o endpoint não existir (fallback para dados hardcoded).
 */
export async function listPlans(): Promise<Plan[]> {
  try {
    // Tentar buscar do backend (endpoint pode não existir ainda)
    return await bffGetJson<Plan[]>("/api/plans");
  } catch (error) {
    if (isNotFoundError(error)) {
      return [];
    }
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
  } catch (error) {
    if (isNotFoundError(error)) {
      return null;
    }
    console.warn(`Erro ao buscar plano ${planId}:`, error);
    return null;
  }
}
