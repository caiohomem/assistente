"use client";

import { useParams } from "next/navigation";
import Link from "next/link";
import { NovaNotaClient } from "./NovaNotaClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function NovaNotaPage() {
  const params = useParams<{ id: string }>();
  const contactId = params?.id;

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
          <NovaNotaClient contactId={contactId} />
        </div>
      </div>
    </LayoutWrapper>
  );
}


