"use client";

import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { listContactsClient, type ListContactsResult } from "@/lib/api/contactsApiClient";
import { ContactsListClient } from "./ContactsListClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { ContactForm, ContactFormData } from "@/components/ContactForm";
import { createContactClient } from "@/lib/api/contactsApiClient";
import { ArrowLeft, Building2, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";

export default function ContactsPage() {
  const [loading, setLoading] = useState(true);
  const [initialData, setInitialData] = useState<ListContactsResult>({
    contacts: [],
    total: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  });
  const searchParams = useSearchParams();
  const router = useRouter();
  const showNewContact = searchParams.get("novo") === "true";
  const companyFilter = searchParams.get("empresa") || "";

  useEffect(() => {
    let isMounted = true;

    async function load() {
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/contatos")}`;
          return;
        }

        const data = await listContactsClient({ page: 1, pageSize: 20 });
        if (!isMounted) return;
        setInitialData(data);
      } catch (e) {
        console.error("Erro ao carregar contatos:", e);
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, []);

  const handleNewContactSubmit = async (formData: ContactFormData) => {
    try {
      await createContactClient({
        firstName: formData.firstName,
        lastName: formData.lastName || null,
        jobTitle: formData.jobTitle || null,
        company: formData.company || null,
        emails: formData.emails.length > 0 ? formData.emails : undefined,
        phones: formData.phones.length > 0 ? formData.phones : undefined,
        address: formData.address,
      });

      // Remove o query param para esconder o formulário e recarrega a lista
      router.push("/contatos");
      router.refresh();
      
      // Recarrega os dados da lista
      const contactsData = await listContactsClient({ page: 1, pageSize: 20 });
      setInitialData(contactsData);
    } catch (error) {
      throw error;
    }
  };

  const handleNewContactCancel = () => {
    router.push("/contatos");
  };

  const handleOpenNewContact = () => {
    router.push("/contatos?novo=true");
  };

  if (loading) {
    return (
      <LayoutWrapper title="Contatos" subtitle="Gerencie sua rede de relacionamentos" activeTab="contacts">
        <div className="flex items-center justify-center py-12">
          <p className="text-muted-foreground">Carregando...</p>
        </div>
      </LayoutWrapper>
    );
  }

  const subtitle = companyFilter
    ? `Contatos da empresa ${companyFilter}`
    : "Gerencie sua rede de relacionamentos";

  return (
    <LayoutWrapper title="Contatos" subtitle={subtitle} activeTab="contacts">
      <div className="space-y-6">
        <div className="flex flex-col items-start gap-3 sm:flex-row sm:items-center sm:justify-between">
          <p className="text-sm text-muted-foreground">
            Gerencie seus contatos e crie novos registros sempre que precisar.
          </p>
          <Button
            variant="glow"
            className="gap-2 rounded-2xl"
            onClick={handleOpenNewContact}
          >
            <Plus className="w-4 h-4" />
            Novo Contato
          </Button>
        </div>

        {/* Company filter banner */}
        {companyFilter && (
          <div className="glass-card p-4 bg-primary/5 border-primary/20 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Building2 className="w-5 h-5 text-primary" />
              <span className="text-sm">
                Filtrando por: <span className="font-medium">{companyFilter}</span>
              </span>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => router.push("/contatos")}
            >
              Ver todos
            </Button>
          </div>
        )}

        {/* Formulário de Novo Contato - aparece quando ?novo=true */}
        {showNewContact && (
          <div className="glass-card p-6 animate-slide-up">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-2xl font-semibold">Novo Contato</h2>
              <Button
                variant="ghost"
                size="icon"
                onClick={handleNewContactCancel}
                className="rounded-lg"
              >
                <ArrowLeft className="w-4 h-4" />
              </Button>
            </div>
            <ContactForm
              onSubmit={handleNewContactSubmit}
              onCancel={handleNewContactCancel}
              submitLabel="Criar Contato"
              cancelLabel="Cancelar"
            />
          </div>
        )}

        {/* Lista de Contatos - sempre visível */}
        <ContactsListClient initialData={initialData} initialCompanyFilter={companyFilter} />
      </div>
    </LayoutWrapper>
  );
}









