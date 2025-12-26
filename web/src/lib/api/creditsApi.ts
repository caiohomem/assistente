import { getBffSession, bffGetJson, bffPostJson } from "../bff";
import type {
  CreditBalance,
  CreditTransaction,
  GrantCreditsRequest,
} from "../types/credit";
import type { CreditPackage } from "../types/plan";

async function getCsrfToken(): Promise<string> {
  const session = await getBffSession();
  return session.csrfToken;
}

export async function getCreditBalance(): Promise<CreditBalance> {
  const csrfToken = await getCsrfToken();
  return bffGetJson<CreditBalance>("/api/credits/balance", csrfToken);
}

export async function listCreditTransactions(): Promise<CreditTransaction[]> {
  const csrfToken = await getCsrfToken();
  return bffGetJson<CreditTransaction[]>("/api/credits/transactions", csrfToken);
}

export async function grantCredits(request: GrantCreditsRequest): Promise<void> {
  const csrfToken = await getCsrfToken();
  return bffPostJson<void>("/api/credits/grant", request, csrfToken);
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
  const csrfToken = await getCsrfToken();
  return bffPostJson<PurchaseCreditPackageResult>(
    "/api/credits/purchase",
    { packageId: request.packageId },
    csrfToken
  );
}

