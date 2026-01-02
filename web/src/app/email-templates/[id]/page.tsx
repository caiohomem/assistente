"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getEmailTemplateByIdClient } from "@/lib/api/emailTemplatesApiClient";
import { EmailTemplateDetailsClient } from "./EmailTemplateDetailsClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function EmailTemplateDetailsPage() {
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
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/email-templates/${templateId}`)}`;
          return;
        }

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

  if (loading) {
    return (
      <LayoutWrapper
        title="Detalhes do Template"
        subtitle="Carregando informações do template"
        activeTab="documents"
      >
        <div className="max-w-4xl mx-auto">
          <div className="glass-card p-8 text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
            <p className="text-muted-foreground">Carregando template...</p>
          </div>
        </div>
      </LayoutWrapper>
    );
  }

  if (error || !template) {
    return (
      <LayoutWrapper
        title="Detalhes do Template"
        subtitle="Erro ao carregar"
        activeTab="documents"
      >
        <div className="max-w-4xl mx-auto">
          <div className="glass-card p-6">
            <div className="rounded-md bg-destructive/10 p-4">
              <p className="text-sm text-destructive">
                {error || "Template não encontrado"}
              </p>
              <Button asChild variant="ghost" className="mt-3">
                <Link href="/email-templates">Voltar para lista</Link>
              </Button>
            </div>
          </div>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper
      title="Detalhes do Template"
      subtitle="Visualize o conteúdo do template de email"
      activeTab="documents"
    >
      <div className="max-w-4xl mx-auto">
        <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
          <Button asChild variant="ghost" className="gap-2">
            <Link href="/email-templates">
              <ArrowLeft className="w-4 h-4" />
              Voltar para templates
            </Link>
          </Button>
          <Button asChild variant="glow">
            <Link href={`/email-templates/${templateId}/editar`}>Editar</Link>
          </Button>
        </div>
        <EmailTemplateDetailsClient template={template} />
      </div>
    </LayoutWrapper>
  );
}
