import { HttpClient } from "./http-client";
import {
  CreditBalance,
  CreditTransaction,
  CreditTransactionType,
  CreditPackage,
  Plan,
} from "./types";

export class CreditsService {
  constructor(private http: HttpClient) {}

  /**
   * Obtém o saldo de créditos do usuário autenticado
   */
  async getBalance(): Promise<CreditBalance> {
    return this.http.get<CreditBalance>("/api/credits/balance");
  }

  /**
   * Lista as transações de crédito do usuário autenticado
   */
  async getTransactions(options?: {
    type?: CreditTransactionType;
    fromDate?: Date;
    toDate?: Date;
    limit?: number;
    offset?: number;
  }): Promise<CreditTransaction[]> {
    const params: any = {};

    if (options?.type) {
      params.type = options.type;
    }
    if (options?.fromDate) {
      params.fromDate = options.fromDate.toISOString();
    }
    if (options?.toDate) {
      params.toDate = options.toDate.toISOString();
    }
    if (options?.limit) {
      params.limit = options.limit;
    }
    if (options?.offset) {
      params.offset = options.offset;
    }

    return this.http.get<CreditTransaction[]>("/api/credits/transactions", params);
  }

  /**
   * Lista os pacotes de créditos disponíveis
   */
  async listPackages(includeInactive?: boolean): Promise<CreditPackage[]> {
    return this.http.get<CreditPackage[]>("/api/credits/packages", {
      includeInactive: includeInactive || false,
    });
  }

  /**
   * Compra um pacote de créditos
   */
  async purchasePackage(packageId: string): Promise<any> {
    return this.http.post("/api/credits/purchase", { packageId });
  }
}

