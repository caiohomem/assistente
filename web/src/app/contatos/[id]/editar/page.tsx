"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import { getContactByIdClient } from "@/lib/api/contactsApiClient";
import { EditarContatoClient } from "./EditarContatoClient";
import { TopBar } from "@/components/TopBar";

export default function EditarContatoPage() {
  const params = useParams<{ id: string }>();
  const contactId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [contact, setContact] = useState<Awaited<ReturnType<typeof getContactByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!contactId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/contatos/${contactId}/editar`)}`;
          return;
        }

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

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Contato" showBackButton backHref={`/contatos/${contactId}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <div className="bg-zinc-800 dark:bg-zinc-800 rounded-lg border border-zinc-700 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-400 dark:text-zinc-400">Carregando...</p>
            ) : error || !contact ? (
              <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
                <p className="text-sm text-red-700 dark:text-red-400">{error ?? "Contato não encontrado."}</p>
                <Link
                  href={`/contatos/${contactId}`}
                  className="mt-3 inline-block text-sm text-red-700 dark:text-red-400 underline"
                >
                  Voltar
                </Link>
              </div>
            ) : (
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
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

