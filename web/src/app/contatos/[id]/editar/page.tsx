"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { getContactByIdClient } from "@/lib/api/contactsApiClient";
import { EditarContatoClient } from "./EditarContatoClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft, AlertCircle } from "lucide-react";

export default function EditarContatoPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const contactId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [contact, setContact] = useState<Awaited<ReturnType<typeof getContactByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!contactId) return;

      try {
        const c = await getContactByIdClient(contactId);
        if (!isMounted) return;
        setContact(c);
      } catch (e) {
        console.error("Erro ao carregar contato:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar contato");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [contactId]);

  if (!contactId) return null;

  const contactName = contact ? `${contact.firstName}${contact.lastName ? ` ${contact.lastName}` : ""}` : "Contato";

  if (loading) {
    return (
      <LayoutWrapper
        title="Editar Contato"
        subtitle="Carregando informações..."
        activeTab="contacts"
      >
        <div className="max-w-2xl mx-auto">
          <div className="glass-card p-8">
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando contato...</span>
            </div>
          </div>
        </div>
      </LayoutWrapper>
    );
  }

  if (error || !contact) {
    return (
      <LayoutWrapper
        title="Editar Contato"
        subtitle="Erro ao carregar"
        activeTab="contacts"
      >
        <div className="max-w-2xl mx-auto">
          <div className="glass-card p-8">
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <div className="w-12 h-12 rounded-full bg-destructive/20 flex items-center justify-center mb-4">
                <AlertCircle className="w-6 h-6 text-destructive" />
              </div>
              <h3 className="text-lg font-semibold text-destructive mb-2">
                {error ?? "Contato não encontrado"}
              </h3>
              <p className="text-sm text-muted-foreground mb-6">
                Não foi possível carregar os dados do contato para edição.
              </p>
              <div className="flex gap-3">
                <Button variant="ghost" onClick={() => router.back()}>
                  <ArrowLeft className="w-4 h-4 mr-2" />
                  Voltar
                </Button>
                <Link href="/contatos">
                  <Button variant="glow">Ver todos contatos</Button>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper
      title={`Editar ${contactName}`}
      subtitle="Atualize as informações do contato"
      activeTab="contacts"
    >
      <div className="max-w-2xl mx-auto">
        {/* Botão Voltar */}
        <div className="mb-6">
          <Button
            variant="ghost"
            onClick={() => router.push(`/contatos/${contactId}`)}
            className="gap-2"
          >
            <ArrowLeft className="w-4 h-4" />
            Voltar para o contato
          </Button>
        </div>

        {/* Formulário */}
        <div className="glass-card p-6">
          <EditarContatoClient
            contactId={contactId}
            initialData={{
              firstName: contact.firstName,
              lastName: contact.lastName || "",
              emails: contact.emails.length > 0 ? contact.emails : [""],
              phones: contact.phones.length > 0 ? contact.phones : [""],
              jobTitle: contact.jobTitle || "",
              company: contact.company || "",
              address: contact.address || {
                street: "",
                city: "",
                state: "",
                zipCode: "",
                country: "",
              },
            }}
          />
        </div>
      </div>
    </LayoutWrapper>
  );
}
