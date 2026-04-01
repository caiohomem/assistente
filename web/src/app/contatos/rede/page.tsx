"use client";

import { useEffect, useState } from "react";
import { getBffSession } from "@/lib/bff";
import { NetworkGraphClient } from "./NetworkGraphClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";

export default function NetworkPage() {
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function check() {
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/contatos/rede")}`;
          return;
        }
      } catch (e) {
        console.error("Erro ao verificar autenticação:", e);
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    check();
    return () => {
      isMounted = false;
    };
  }, []);

  if (loading) {
    return (
      <LayoutWrapper title="Rede de Relacionamentos" subtitle="Visualize sua rede de contatos" activeTab="network">
        <div className="flex items-center justify-center py-12">
          <p className="text-muted-foreground">Carregando...</p>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper title="Rede de Relacionamentos" subtitle="Visualize sua rede de contatos" activeTab="network">
      <NetworkGraphClient />
    </LayoutWrapper>
  );
}




