"use client";

import { useEffect, useState } from "react";
import { useParams, useSearchParams, useRouter } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import type { Contact } from "@/lib/types/contact";
import { getContactByIdClient } from "@/lib/api/contactsApiClient";
import { listNotesByContactClient } from "@/lib/api/notesApiClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { ContactDetailsClient } from "./ContactDetailsClient";

export default function ContactDetailsPage() {
  const params = useParams<{ id: string }>();
  const searchParams = useSearchParams();
  const router = useRouter();
  const contactId = params?.id;
  const showNewNote = searchParams.get("novo") === "true";
  const showNewAudioNote = searchParams.get("audio") === "true";

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

  const loadNotes = async () => {
    if (!contactId) return;
    try {
      const n = await listNotesByContactClient(contactId);
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
      console.error("Erro ao carregar notas:", e);
    }
  };

  const handleNewNoteSuccess = async () => {
    router.push(`/contatos/${contactId}`);
    router.refresh();
    await loadNotes();
  };

  const handleNewNoteCancel = () => {
    router.push(`/contatos/${contactId}`);
  };

  const handleNewAudioNoteSuccess = async () => {
    router.push(`/contatos/${contactId}`);
    router.refresh();
    await loadNotes();
  };

  const handleNewAudioNoteCancel = () => {
    router.push(`/contatos/${contactId}`);
  };

  if (!contactId) return null;

  if (loading) {
    return (
      <LayoutWrapper title="Carregando..." activeTab="contacts">
        <div className="flex items-center justify-center py-12">
          <p className="text-muted-foreground">Carregando...</p>
        </div>
      </LayoutWrapper>
    );
  }

  if (error || !contact) {
    return (
      <LayoutWrapper title="Erro" activeTab="contacts">
        <div className="max-w-4xl">
          <div className="glass-card border-destructive/50 bg-destructive/10 p-6">
            <h2 className="text-lg font-semibold text-destructive mb-2">Erro ao carregar contato</h2>
            <p className="text-sm text-destructive/80 mb-4">{error ?? "Erro desconhecido"}</p>
            <Link href="/contatos" className="text-sm text-primary hover:underline">
              Voltar para lista de contatos
            </Link>
          </div>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper 
      title={contact.fullName} 
      subtitle="Detalhes do contato" 
      activeTab="contacts"
    >
      <div className="max-w-4xl">
        <ContactDetailsClient 
          contactId={contactId} 
          contact={contact} 
          notes={notes}
          showNewNote={showNewNote}
          onNewNoteSuccess={handleNewNoteSuccess}
          onNewNoteCancel={handleNewNoteCancel}
          showNewAudioNote={showNewAudioNote}
          onNewAudioNoteSuccess={handleNewAudioNoteSuccess}
          onNewAudioNoteCancel={handleNewAudioNoteCancel}
        />
      </div>
    </LayoutWrapper>
  );
}

