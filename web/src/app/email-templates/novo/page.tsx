"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { NovoEmailTemplateClient } from "./NovoEmailTemplateClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function NovoEmailTemplatePage() {
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    async function check() {
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/email-templates/novo")}`;
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
  }, []);

  return (
    <LayoutWrapper
      title="Novo Template de Email"
      subtitle="Crie um novo template de email"
      activeTab="documents"
    >
      <div className="max-w-4xl mx-auto">
        <div className="mb-6">
          <Button asChild variant="ghost" className="gap-2">
            <Link href="/email-templates">
              <ArrowLeft className="w-4 h-4" />
              Voltar para templates
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
            <NovoEmailTemplateClient />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
