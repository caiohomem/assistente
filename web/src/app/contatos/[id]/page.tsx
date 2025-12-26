"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import type { Contact } from "@/lib/types/contact";
import { getContactByIdClient } from "@/lib/api/contactsApiClient";
import { listNotesByContactClient } from "@/lib/api/notesApiClient";
import { TopBar } from "@/components/TopBar";
import { ContactDetailsClient } from "./ContactDetailsClient";

export default function ContactDetailsPage() {
  const params = useParams<{ id: string }>();
  const contactId = params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [contact, setContact] = useState<Contact | null>(null);
  const [notes, setNotes] = useState<
    Array<{
      noteId: string;
      type: number;
      rawContent: string;
      structuredData?: string | null;
      createdAt: string;
    }>
  >([]);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!contactId) return;

      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(`/contatos/${contactId}`)}`;
          return;
        }

        const [c, n] = await Promise.all([
          getContactByIdClient(contactId),
          listNotesByContactClient(contactId),
        ]);

        if (!isMounted) return;

        setContact(c);
        setNotes(
          n.map((note) => ({
            noteId: note.noteId,
            type: note.type,
            rawContent: note.rawContent,
            structuredData: note.structuredData,
            createdAt: note.createdAt,
          })),
        );
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

  if (loading) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
        <TopBar title="Carregando..." showBackButton backHref="/contatos" />
        <main className="container mx-auto px-4 py-8">
          <p className="text-sm text-zinc-600 dark:text-zinc-400">Carregando...</p>
        </main>
      </div>
    );
  }

  if (error || !contact) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
        <TopBar title="Erro" showBackButton backHref="/contatos" />
        <main className="container mx-auto px-4 py-8">
          <div className="max-w-4xl mx-auto">
            <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
              <h2 className="text-lg font-semibold text-red-900 dark:text-red-400">Erro ao carregar contato</h2>
              <p className="mt-2 text-sm text-red-700 dark:text-red-400">{error ?? "Erro desconhecido"}</p>
              <Link href="/contatos" className="mt-4 inline-block text-sm text-red-700 dark:text-red-400 underline">
                Voltar para lista de contatos
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
      <TopBar title={contact.fullName} showBackButton backHref="/contatos" />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <ContactDetailsClient contactId={contactId} contact={contact} notes={notes} />
        </div>
      </main>
    </div>
  );
}

