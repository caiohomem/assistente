"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { NovaNotaClient } from "./NovaNotaClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function NovaNotaPage() {
  const params = useParams<{ id: string }>();
  const contactId = params?.id;
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function check() {
      if (!contactId) return;
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/contatos/${contactId}/notas/novo`)}`;
          return;
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    check();
    return () => {
      isMounted = false;
    };
  }, [contactId]);

  if (!contactId) return null;

  return (
    <LayoutWrapper
      title="Nova Nota"
      subtitle="Registre uma nova nota para o contato"
      activeTab="notes"
    >
      <div className="max-w-2xl mx-auto">
        <div className="mb-6">
          <Button asChild variant="ghost" className="gap-2">
            <Link href={`/contatos/${contactId}`}>
              <ArrowLeft className="w-4 h-4" />
              Voltar para o contato
            </Link>
          </Button>
        </div>
        <div className="glass-card p-6">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando...</span>
            </div>
          ) : (
            <NovaNotaClient contactId={contactId} />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}


