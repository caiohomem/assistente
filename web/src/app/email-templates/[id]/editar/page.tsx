"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getEmailTemplateByIdClient } from "@/lib/api/emailTemplatesApiClient";
import { EditarEmailTemplateClient } from "./EditarEmailTemplateClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function EditarEmailTemplatePage() {
  const params = useParams<{ id: string }>();
  const templateId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [template, setTemplate] = useState<Awaited<ReturnType<typeof getEmailTemplateByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!templateId) return;

      try {
        const t = await getEmailTemplateByIdClient(templateId);
        if (!isMounted) return;
        setTemplate(t);
      } catch (e) {
        console.error("Erro ao carregar template de email:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar template de email");
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
      title="Editar Template de Email"
      subtitle="Atualize o conteúdo do template"
      activeTab="documents"
    >
      <div className="max-w-4xl mx-auto">
        <div className="mb-6">
          <Link href={`/email-templates/${templateId}`}>
            <Button variant="ghost" className="gap-2">
              <ArrowLeft className="w-4 h-4" />
              Voltar para o template
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
              <p className="text-sm text-destructive">{error || "Template não encontrado"}</p>
              <Link
                href="/email-templates"
                className="mt-3 inline-block text-sm text-destructive underline"
              >
                Voltar para lista
              </Link>
            </div>
          ) : (
            <EditarEmailTemplateClient templateId={templateId} initialData={template} />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
