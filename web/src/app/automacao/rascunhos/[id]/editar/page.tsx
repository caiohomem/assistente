"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getDraftByIdClient } from "@/lib/api/automationApiClient";
import { EditarRascunhoClient } from "./EditarRascunhoClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function EditarRascunhoPage() {
  const params = useParams<{ id: string }>();
  const draftId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [draft, setDraft] = useState<Awaited<ReturnType<typeof getDraftByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!draftId) return;

      try {
        const d = await getDraftByIdClient(draftId);
        if (!isMounted) return;
        setDraft(d);
      } catch (e) {
        console.error("Erro ao carregar rascunho:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar rascunho");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [draftId]);

  if (!draftId) return null;

  return (
    <LayoutWrapper
      title="Editar Rascunho"
      subtitle="Atualize o conteúdo do rascunho"
      activeTab="documents"
    >
      <div className="max-w-4xl mx-auto">
        <div className="mb-6">
          <Link href="/automacao/rascunhos">
            <Button variant="ghost" className="gap-2">
              <ArrowLeft className="w-4 h-4" />
              Voltar para rascunhos
            </Button>
          </Link>
        </div>
        <div className="glass-card p-6">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando rascunho...</span>
            </div>
          ) : error || !draft ? (
            <div className="rounded-md bg-destructive/10 p-4">
              <p className="text-sm text-destructive">{error ?? "Rascunho não encontrado."}</p>
              <Link
                href="/automacao/rascunhos"
                className="mt-3 inline-block text-sm text-destructive underline"
              >
                Voltar
              </Link>
            </div>
          ) : (
            <EditarRascunhoClient
              draftId={draftId}
              initialData={{
                documentType: draft.documentType,
                content: draft.content,
                contactId: draft.contactId || "",
                templateId: draft.templateId || "",
                letterheadId: draft.letterheadId || "",
                status: draft.status,
              }}
            />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
