"use client";

import Link from "next/link";
import { NovoEmailTemplateClient } from "./NovoEmailTemplateClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function NovoEmailTemplatePage() {
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
          <NovoEmailTemplateClient />
        </div>
      </div>
    </LayoutWrapper>
  );
}
