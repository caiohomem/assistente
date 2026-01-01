export interface Plan {
  planId: string;
  name: string;
  price: number;
  currency: string;
  interval: "monthly" | "yearly";
  features: string[];
  limits?: {
    contacts?: number | null;
    notes?: number | null;
    creditsPerMonth?: number | null;
    storageGB?: number | null;
  };
  isActive: boolean;
  highlighted?: boolean;
}

export interface CreditPackage {
  packageId: string;
  name: string;
  amount: number; // -1 para ilimitado
  price: number;
  currency: string;
  description?: string | null;
  isActive: boolean;
}












