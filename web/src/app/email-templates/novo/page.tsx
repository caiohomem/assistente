"use client";

import { useEffect, useState } from "react";
import { getBffSession } from "@/lib/bff";
import { NovoEmailTemplateClient } from "./NovoEmailTemplateClient";
import { TopBar } from "@/components/TopBar";

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
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Novo Template de Email" showBackButton backHref="/email-templates" />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            {loading ? (
              <p className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</p>
            ) : (
              <NovoEmailTemplateClient />
            )}
          </div>
        </div>
      </main>
    </div>
  );
}

