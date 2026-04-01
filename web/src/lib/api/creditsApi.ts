import { bffGetJson, bffPostJson } from "../bff";
import type {
  CreditBalance,
  CreditTransaction,
  GrantCreditsRequest,
} from "../types/credit";
import type { CreditPackage } from "../types/plan";

export async function getCreditBalance(): Promise<CreditBalance> {
  return bffGetJson<CreditBalance>("/api/credits/balance");
}

export async function listCreditTransactions(): Promise<CreditTransaction[]> {
  return bffGetJson<CreditTransaction[]>("/api/credits/transactions");
}

export async function grantCredits(request: GrantCreditsRequest): Promise<void> {
  return bffPostJson<void>("/api/credits/grant", request, "");
}

/**
 * Lista todos os pacotes de créditos disponíveis.
 * Retorna array vazio se o endpoint não existir (fallback para dados hardcoded).
 */
export async function listCreditPackages(): Promise<CreditPackage[]> {
  try {
    // Tentar buscar do backend (endpoint pode não existir ainda)
    return await bffGetJson<CreditPackage[]>("/api/credits/packages");
  } catch (error) {
    // Se o endpoint não existir, retornar array vazio
    // A landing page usará dados hardcoded como fallback
    console.warn("Endpoint /api/credits/packages não disponível, usando dados hardcoded:", error);
    return [];
  }
}

export interface PurchaseCreditPackageRequest {
  packageId: string;
}

export interface PurchaseCreditPackageResult {
  ownerUserId: string;
  newBalance: number;
  transactionId: string;
  packageName: string;
  creditsAdded: number;
}

/**
 * Compra um pacote de créditos (adiciona créditos diretamente)
 */
export async function purchaseCreditPackage(
  request: PurchaseCreditPackageRequest
): Promise<PurchaseCreditPackageResult> {
  return bffPostJson<PurchaseCreditPackageResult>(
    "/api/credits/purchase",
    { packageId: request.packageId },
    ""
  );
}
