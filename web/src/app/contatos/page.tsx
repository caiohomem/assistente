
"use client";

import { useEffect, useState } from "react";
import { getBffSession } from "@/lib/bff";
import { listContactsClient, type ListContactsResult } from "@/lib/api/contactsApiClient";
import { ContactsListClient } from "./ContactsListClient";

export default function ContactsPage() {
  const [loading, setLoading] = useState(true);
  const [initialData, setInitialData] = useState<ListContactsResult>({
    contacts: [],
    total: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  });

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

  if (loading) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <p className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</p>
        </div>
      </div>
    );
  }

  return <ContactsListClient initialData={initialData} />;
}





