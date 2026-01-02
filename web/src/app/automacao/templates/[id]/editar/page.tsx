"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getTemplateByIdClient } from "@/lib/api/automationApiClient";
import { EditarTemplateClient } from "./EditarTemplateClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function EditarTemplatePage() {
  const params = useParams<{ id: string }>();
  const templateId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [template, setTemplate] = useState<Awaited<ReturnType<typeof getTemplateByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!templateId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/automacao/templates/${templateId}/editar`)}`;
          return;
        }

        const t = await getTemplateByIdClient(templateId);
        if (!isMounted) return;
        setTemplate(t);
      } catch (e) {
        console.error("Erro ao carregar template:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar template");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [templateId]);

  if (!templateId) return null;

  return (
    <LayoutWrapper
      title="Editar Template"
      subtitle="Atualize os detalhes do template"
      activeTab="documents"
    >
      <div className="max-w-4xl mx-auto">
        <div className="mb-6">
          <Link href="/automacao/templates">
            <Button variant="ghost" className="gap-2">
              <ArrowLeft className="w-4 h-4" />
              Voltar para templates
            </Button>
          </Link>
        </div>
        <div className="glass-card p-6">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando template...</span>
            </div>
          ) : error || !template ? (
            <div className="rounded-md bg-destructive/10 p-4">
              <p className="text-sm text-destructive">{error ?? "Template nAśo encontrado."}</p>
              <Link
                href="/automacao/templates"
                className="mt-3 inline-block text-sm text-destructive underline"
              >
                Voltar
              </Link>
            </div>
          ) : (
            <EditarTemplateClient
              templateId={templateId}
              initialData={{
                name: template.name,
                type: template.type,
                body: template.body,
                placeholdersSchema: template.placeholdersSchema || "",
                active: template.active,
              }}
            />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
