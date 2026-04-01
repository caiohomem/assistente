"use client";

import Link from "next/link";
import { NovoContatoClient } from "./NovoContatoClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function NovoContatoPage() {
  return (
    <LayoutWrapper
      title="Novo Contato"
      subtitle="Adicione um novo contato"
      activeTab="contacts"
    >
      <div className="max-w-2xl mx-auto">
        <div className="mb-6">
          <Button asChild variant="ghost" className="gap-2">
            <Link href="/contatos">
              <ArrowLeft className="w-4 h-4" />
              Voltar para contatos
            </Link>
          </Button>
        </div>
        <div className="glass-card p-6">
          <NovoContatoClient />
        </div>
      </div>
    </LayoutWrapper>
  );
}
