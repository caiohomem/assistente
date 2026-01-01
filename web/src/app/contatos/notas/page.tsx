"use client";

import { useEffect, useState } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { listContactsClient } from "@/lib/api/contactsApiClient";
import { listNotesByContactClient } from "@/lib/api/notesApiClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { SearchableContactSelect } from "@/components/SearchableContactSelect";
import { NotasClient } from "./NotasClient";
import type { Contact } from "@/lib/types/contact";
import type { Note } from "@/lib/types/note";

export default function NotasPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const showNewNote = searchParams.get("novo") === "true";
  const showNewAudioNote = searchParams.get("audio") === "true";
  const [loading, setLoading] = useState(true);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [selectedContactId, setSelectedContactId] = useState<string>("");
  const [notes, setNotes] = useState<Note[]>([]);
  const [loadingNotes, setLoadingNotes] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/contatos/notas")}`;
          return;
        }

        const data = await listContactsClient({ page: 1, pageSize: 1000 });
        if (!isMounted) return;
        setContacts(data.contacts);
      } catch (e) {
        console.error("Erro ao carregar contatos:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar contatos");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    if (!selectedContactId) {
      setNotes([]);
      return;
    }

    let isMounted = true;

    async function loadNotes() {
      setLoadingNotes(true);
      setError(null);
      try {
        const notesData = await listNotesByContactClient(selectedContactId);
        if (!isMounted) return;
        setNotes(notesData);
      } catch (e) {
        console.error("Erro ao carregar notas:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar notas");
      } finally {
        if (isMounted) setLoadingNotes(false);
      }
    }

    loadNotes();
    return () => {
      isMounted = false;
    };
  }, [selectedContactId]);

  const loadNotes = async () => {
    if (!selectedContactId) return;
    try {
      const notesData = await listNotesByContactClient(selectedContactId);
      setNotes(notesData);
    } catch (e) {
      console.error("Erro ao carregar notas:", e);
      setError(e instanceof Error ? e.message : "Erro ao carregar notas");
    }
  };

  const handleNewNoteSuccess = async () => {
    router.push("/contatos/notas");
    router.refresh();
    if (selectedContactId) {
      await loadNotes();
    }
  };

  const handleNewNoteCancel = () => {
    router.push("/contatos/notas");
  };

  const handleNewAudioNoteSuccess = async () => {
    router.push("/contatos/notas");
    router.refresh();
    if (selectedContactId) {
      await loadNotes();
    }
  };

  const handleNewAudioNoteCancel = () => {
    router.push("/contatos/notas");
  };

  if (loading) {
    return (
      <LayoutWrapper title="Notas" subtitle="Visualize e gerencie notas de contatos" activeTab="notes">
        <div className="flex items-center justify-center py-12">
          <p className="text-muted-foreground">Carregando...</p>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper title="Notas" subtitle="Visualize e gerencie notas de contatos" activeTab="notes">
      <div className="space-y-6">
        {/* Seletor de Contato */}
        <div className="glass-card p-6">
          <label className="block text-sm font-medium text-foreground mb-3">
            Selecione um Contato
          </label>
          <SearchableContactSelect
            contacts={contacts}
            value={selectedContactId}
            onChange={setSelectedContactId}
            placeholder="Selecione um contato para ver suas notas"
            className="w-full"
          />
        </div>

        {/* Conteúdo das Notas */}
        {selectedContactId ? (
          <NotasClient
            contactId={selectedContactId}
            notes={notes}
            loading={loadingNotes}
            error={error}
            onNotesChange={setNotes}
            showNewNote={showNewNote}
            onNewNoteSuccess={handleNewNoteSuccess}
            onNewNoteCancel={handleNewNoteCancel}
            showNewAudioNote={showNewAudioNote}
            onNewAudioNoteSuccess={handleNewAudioNoteSuccess}
            onNewAudioNoteCancel={handleNewAudioNoteCancel}
          />
        ) : (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground">
              Selecione um contato acima para visualizar suas notas
            </p>
          </div>
        )}
      </div>
    </LayoutWrapper>
  );
}

