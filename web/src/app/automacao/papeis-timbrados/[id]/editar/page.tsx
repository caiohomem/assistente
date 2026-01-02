"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getLetterheadByIdClient } from "@/lib/api/automationApiClient";
import { EditarPapelTimbradoClient } from "./EditarPapelTimbradoClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function EditarPapelTimbradoPage() {
  const params = useParams<{ id: string }>();
  const letterheadId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [letterhead, setLetterhead] = useState<Awaited<ReturnType<typeof getLetterheadByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!letterheadId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/automacao/papeis-timbrados/${letterheadId}/editar`)}`;
          return;
        }

        const l = await getLetterheadByIdClient(letterheadId);
        if (!isMounted) return;
        setLetterhead(l);
      } catch (e) {
        console.error("Erro ao carregar papel timbrado:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar papel timbrado");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [letterheadId]);

  if (!letterheadId) return null;

  return (
    <LayoutWrapper
      title="Editar Papel Timbrado"
      subtitle="Atualize os dados do papel timbrado"
      activeTab="documents"
    >
      <div className="max-w-4xl mx-auto">
        <div className="mb-6">
          <Link href="/automacao/papeis-timbrados">
            <Button variant="ghost" className="gap-2">
              <ArrowLeft className="w-4 h-4" />
              Voltar para papeis timbrados
            </Button>
          </Link>
        </div>
        <div className="glass-card p-6">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando papel timbrado...</span>
            </div>
          ) : error || !letterhead ? (
            <div className="rounded-md bg-destructive/10 p-4">
              <p className="text-sm text-destructive">{error ?? "Papel timbrado não encontrado."}</p>
              <Link
                href="/automacao/papeis-timbrados"
                className="mt-3 inline-block text-sm text-destructive underline"
              >
                Voltar
              </Link>
            </div>
          ) : (
            <EditarPapelTimbradoClient
              letterheadId={letterheadId}
              initialData={{
                name: letterhead.name,
                designData: letterhead.designData,
                isActive: letterhead.isActive,
              }}
            />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
