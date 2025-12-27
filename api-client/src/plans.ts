import { HttpClient } from "./http-client";
import { Plan } from "./types";

export class PlansService {
  constructor(private http: HttpClient) {}

  /**
   * Lista todos os planos disponíveis
   */
  async list(includeInactive?: boolean): Promise<Plan[]> {
    return this.http.get<Plan[]>("/api/plans", {
      includeInactive: includeInactive || false,
    });
  }
}

