"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { NovoRelacionamentoClient } from "./NovoRelacionamentoClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function NovoRelacionamentoPage() {
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
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/contatos/${contactId}/relacionamentos/novo`)}`;
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
      title="Novo Relacionamento"
      subtitle="Registre um novo relacionamento"
      activeTab="contacts"
    >
      <div className="max-w-4xl mx-auto w-full">
        <div className="mb-8 flex items-center justify-between">
          <Button
            asChild
            variant="ghost"
            className="gap-2 text-slate-200 hover:text-white hover:bg-white/10"
          >
            <Link href={`/contatos/${contactId}`}>
              <ArrowLeft className="w-4 h-4" />
              Voltar para o contato
            </Link>
          </Button>
        </div>

        <div className="relative overflow-hidden rounded-[32px] border border-white/10 bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 p-6 sm:p-10 shadow-[0_30px_80px_rgba(15,23,42,0.6)]">
          <div
            className="pointer-events-none absolute inset-0 opacity-40"
            style={{
              backgroundImage:
                "linear-gradient(transparent 0 95%, rgba(255,255,255,0.05) 96%),linear-gradient(90deg,transparent 0 95%, rgba(255,255,255,0.05) 96%)",
              backgroundSize: "80px 80px",
            }}
          />
          <div className="pointer-events-none absolute -inset-x-16 -top-40 h-64 bg-[radial-gradient(circle,rgba(59,130,246,0.35),transparent_55%)] blur-3xl opacity-70" />
          <div className="relative z-10">
            {loading ? (
              <div className="flex flex-col items-center justify-center py-16 text-slate-300">
                <div className="mb-4 h-10 w-10 animate-spin rounded-full border-2 border-white/10 border-t-sky-400" />
                <span className="text-sm text-slate-400">Carregando relacionamento...</span>
              </div>
            ) : (
              <NovoRelacionamentoClient contactId={contactId} />
            )}
          </div>
        </div>
      </div>
    </LayoutWrapper>
  );
}

