"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { deleteNoteClient } from "@/lib/api/notesApiClient";
import { NoteType, type Note } from "@/lib/types/note";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { Button } from "@/components/ui/button";
import { Trash2, Mic, FileText, Plus, ArrowLeft } from "lucide-react";
import { cn } from "@/lib/utils";
import { AudioPlayer } from "./AudioPlayer";
import { NovaNotaClient } from "../[id]/notas/novo/NovaNotaClient";
import { NovaNotaAudioClient } from "../[id]/notas-audio/NovaNotaAudioClient";

type TabType = "audio" | "text";

interface NotasClientProps {
  contactId: string;
  notes: Note[];
  loading: boolean;
  error: string | null;
  onNotesChange: (notes: Note[]) => void;
  showNewNote?: boolean;
  onNewNoteSuccess?: () => void;
  onNewNoteCancel?: () => void;
  showNewAudioNote?: boolean;
  onNewAudioNoteSuccess?: () => void;
  onNewAudioNoteCancel?: () => void;
}

function formatDate(dateString: string): string {
  const date = new Date(dateString);
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(date);
}

export function NotasClient({
  contactId,
  notes,
  loading,
  error,
  onNotesChange,
  showNewNote = false,
  onNewNoteSuccess,
  onNewNoteCancel,
  showNewAudioNote = false,
  onNewAudioNoteSuccess,
  onNewAudioNoteCancel,
}: NotasClientProps) {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState<TabType>("audio");
  const [deletingNoteId, setDeletingNoteId] = useState<string | null>(null);
  const [confirmDeleteNote, setConfirmDeleteNote] = useState<string | null>(null);

  const audioNotes = notes.filter((note) => note.type === NoteType.Audio);
  const textNotes = notes.filter((note) => note.type === NoteType.Text);

  const handleDeleteNote = async (noteId: string) => {
    setDeletingNoteId(noteId);
    try {
      await deleteNoteClient(noteId);
      onNotesChange(notes.filter((note) => note.noteId !== noteId));
      setConfirmDeleteNote(null);
    } catch (err) {
      console.error("Erro ao deletar nota:", err);
    } finally {
      setDeletingNoteId(null);
    }
  };

  if (loading) {
    return (
      <div className="glass-card p-12 text-center">
        <p className="text-muted-foreground">Carregando notas...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="glass-card border-destructive/50 bg-destructive/10 p-6">
        <p className="text-sm text-destructive">{error}</p>
      </div>
    );
  }

  return (
    <>
      {/* Formulário de Nova Nota - aparece quando ?novo=true */}
      {showNewNote && (
        <div className="glass-card p-6 animate-slide-up mb-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl font-semibold">Nova Nota</h2>
            <Button
              variant="ghost"
              size="icon"
              onClick={onNewNoteCancel}
              className="rounded-lg"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </div>
          <NovaNotaClient 
            contactId={contactId} 
            onSuccess={onNewNoteSuccess} 
            onCancel={onNewNoteCancel} 
          />
        </div>
      )}

      {/* Formulário de Nova Nota de Áudio - aparece quando ?audio=true */}
      {showNewAudioNote && (
        <div className="glass-card p-6 animate-slide-up mb-6">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl font-semibold">Nova Nota de Áudio</h2>
            <Button
              variant="ghost"
              size="icon"
              onClick={onNewAudioNoteCancel}
              className="rounded-lg"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </div>
          <NovaNotaAudioClient 
            contactId={contactId} 
            onSuccess={onNewAudioNoteSuccess} 
            onCancel={onNewAudioNoteCancel} 
          />
        </div>
      )}

      <div className="glass-card p-6">
        {/* Tabs */}
        <div className="flex gap-2 border-b border-border overflow-x-auto mb-6">
          <button
            onClick={() => setActiveTab("audio")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "audio"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <Mic className="w-4 h-4" />
              Notas de Áudio ({audioNotes.length})
            </div>
          </button>
          <button
            onClick={() => setActiveTab("text")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "text"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <FileText className="w-4 h-4" />
              Notas de Texto ({textNotes.length})
            </div>
          </button>
        </div>

        {/* Content */}
        {activeTab === "audio" && (
          <div className="space-y-4">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold mb-2">Notas de Áudio</h3>
                <p className="text-sm text-muted-foreground">
                  Visualize e gerencie as notas de áudio deste contato.
                </p>
              </div>
              <Button
                onClick={() => router.push(`/contatos/notas?audio=true`)}
                variant="glow"
              >
                <Plus className="w-4 h-4 mr-2" />
                Nova Nota de Áudio
              </Button>
            </div>

            {audioNotes.length === 0 ? (
              <div className="text-center py-12">
                <Mic className="w-12 h-12 mx-auto text-muted-foreground mb-4 opacity-50" />
                <p className="text-sm text-muted-foreground mb-4">
                  Nenhuma nota de áudio cadastrada.
                </p>
                <Button
                  onClick={() => router.push(`/contatos/notas?audio=true`)}
                  variant="glow"
                >
                  <Plus className="w-4 h-4 mr-2" />
                  Criar Primeira Nota de Áudio
                </Button>
              </div>
            ) : (
              <div className="space-y-4">
                {audioNotes.map((note) => (
                  <div
                    key={note.noteId}
                    className="glass-card border-border/50 p-4"
                  >
                    <div className="mb-2 flex items-center justify-between flex-wrap gap-2">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="rounded-full bg-accent/10 text-accent px-3 py-1 text-xs font-medium">
                          Áudio
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {formatDate(note.createdAt)}
                        </span>
                      </div>
                      <Button
                        onClick={() => setConfirmDeleteNote(note.noteId)}
                        disabled={deletingNoteId === note.noteId}
                        variant="destructive"
                        size="sm"
                        title="Excluir nota"
                      >
                        {deletingNoteId === note.noteId ? (
                          "Excluindo..."
                        ) : (
                          <>
                            <Trash2 className="w-4 h-4 mr-1" />
                            Excluir
                          </>
                        )}
                      </Button>
                    </div>
                    <AudioPlayer noteId={note.noteId} />
                    {note.rawContent && (
                      <div className="mt-3 glass-card border-primary/20 bg-primary/5 p-3">
                        <div className="mb-2 flex items-center gap-2">
                          <Mic className="h-4 w-4 text-primary" />
                          <span className="text-xs font-medium text-primary">
                            Transcrição de Áudio
                          </span>
                        </div>
                        <p className="whitespace-pre-wrap text-sm leading-relaxed">
                          {note.rawContent}
                        </p>
                      </div>
                    )}
                    {note.structuredData && (
                      <details className="mt-2">
                        <summary className="cursor-pointer text-xs text-muted-foreground hover:text-foreground transition-colors">
                          Ver dados estruturados
                        </summary>
                        <div className="mt-2">
                          <pre className="overflow-auto rounded-lg glass-card p-3 text-xs font-mono">
                            {JSON.stringify(JSON.parse(note.structuredData), null, 2)}
                          </pre>
                        </div>
                      </details>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {activeTab === "text" && (
          <div className="space-y-4">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg font-semibold mb-2">Notas de Texto</h3>
                <p className="text-sm text-muted-foreground">
                  Visualize e gerencie as notas de texto deste contato.
                </p>
              </div>
              <Button
                onClick={() => router.push(`/contatos/notas?novo=true`)}
                variant="glow"
              >
                <Plus className="w-4 h-4 mr-2" />
                Nova Nota de Texto
              </Button>
            </div>

            {textNotes.length === 0 ? (
              <div className="text-center py-12">
                <FileText className="w-12 h-12 mx-auto text-muted-foreground mb-4 opacity-50" />
                <p className="text-sm text-muted-foreground mb-4">
                  Nenhuma nota de texto cadastrada.
                </p>
                <Button
                  onClick={() => router.push(`/contatos/notas?novo=true`)}
                  variant="glow"
                >
                  <Plus className="w-4 h-4 mr-2" />
                  Criar Primeira Nota de Texto
                </Button>
              </div>
            ) : (
              <div className="space-y-4">
                {textNotes.map((note) => (
                  <div
                    key={note.noteId}
                    className="glass-card border-border/50 p-4"
                  >
                    <div className="mb-2 flex items-center justify-between flex-wrap gap-2">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="rounded-full bg-accent/10 text-accent px-3 py-1 text-xs font-medium">
                          Texto
                        </span>
                        <span className="text-xs text-muted-foreground">
                          {formatDate(note.createdAt)}
                        </span>
                      </div>
                      <Button
                        onClick={() => setConfirmDeleteNote(note.noteId)}
                        disabled={deletingNoteId === note.noteId}
                        variant="destructive"
                        size="sm"
                        title="Excluir nota"
                      >
                        {deletingNoteId === note.noteId ? (
                          "Excluindo..."
                        ) : (
                          <>
                            <Trash2 className="w-4 h-4 mr-1" />
                            Excluir
                          </>
                        )}
                      </Button>
                    </div>
                    <p className="whitespace-pre-wrap text-sm">
                      {note.rawContent}
                    </p>
                    {note.structuredData && (
                      <details className="mt-2">
                        <summary className="cursor-pointer text-xs text-muted-foreground hover:text-foreground transition-colors">
                          Ver dados estruturados
                        </summary>
                        <div className="mt-2">
                          <pre className="overflow-auto rounded-lg glass-card p-3 text-xs font-mono">
                            {JSON.stringify(JSON.parse(note.structuredData), null, 2)}
                          </pre>
                        </div>
                      </details>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>

      {/* Confirm Delete Dialog */}
      <ConfirmDialog
        isOpen={confirmDeleteNote !== null}
        onCancel={() => setConfirmDeleteNote(null)}
        onConfirm={() => {
          if (confirmDeleteNote) {
            handleDeleteNote(confirmDeleteNote);
          }
        }}
        title="Excluir Nota"
        message="Tem certeza que deseja excluir esta nota? Esta ação não pode ser desfeita."
        confirmText="Excluir"
        cancelText="Cancelar"
        variant="danger"
      />
    </>
  );
}

