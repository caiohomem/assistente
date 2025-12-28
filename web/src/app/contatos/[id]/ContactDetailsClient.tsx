"use client";

import { useState, useRef, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { deleteContactClient, deleteRelationshipClient } from "@/lib/api/contactsApiClient";
import { deleteNoteClient } from "@/lib/api/notesApiClient";
import type { Contact, Relationship } from "@/lib/types/contact";
import { NoteType } from "@/lib/types/note";
import { getApiBaseUrl } from "@/lib/bff";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { Button } from "@/components/ui/button";
import { Edit, Trash2, Mail, Phone, Building2, MapPin, Calendar, Tag, Link as LinkIcon, Plus, Mic, ArrowLeft } from "lucide-react";
import { NovaNotaClient } from "./notas/novo/NovaNotaClient";
import { NovaNotaAudioClient } from "./notas-audio/NovaNotaAudioClient";

function formatTime(seconds: number): string {
  if (isNaN(seconds)) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

interface ContactDetailsClientProps {
  contactId: string;
  contact: Contact;
  notes: Array<{
    noteId: string;
    type: NoteType;
    rawContent: string;
    structuredData?: string | null;
    createdAt: string;
  }>;
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

function formatAddress(address: Contact["address"]): string {
  if (!address) return "Não informado";
  const parts: string[] = [];
  if (address.street) parts.push(address.street);
  if (address.city) parts.push(address.city);
  if (address.state) parts.push(address.state);
  if (address.zipCode) parts.push(address.zipCode);
  if (address.country) parts.push(address.country);
  return parts.length > 0 ? parts.join(", ") : "Não informado";
}

function getNoteTypeLabel(type: NoteType): string {
  switch (type) {
    case NoteType.Audio:
      return "Áudio";
    case NoteType.Text:
      return "Texto";
    default:
      return "Desconhecido";
  }
}

export function ContactDetailsClient({
  contactId,
  contact,
  notes,
  showNewNote = false,
  onNewNoteSuccess,
  onNewNoteCancel,
  showNewAudioNote = false,
  onNewAudioNoteSuccess,
  onNewAudioNoteCancel,
}: ContactDetailsClientProps) {
  const router = useRouter();
  const [contactState, setContactState] = useState(contact);
  const [notesState, setNotesState] = useState(notes);
  const [isDeleting, setIsDeleting] = useState(false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deletingNoteId, setDeletingNoteId] = useState<string | null>(null);
  const [deletingRelationshipId, setDeletingRelationshipId] = useState<string | null>(null);
  const [confirmDeleteNote, setConfirmDeleteNote] = useState<string | null>(null);
  const [confirmDeleteRelationship, setConfirmDeleteRelationship] = useState<string | null>(null);

  useEffect(() => {
    setContactState(contact);
  }, [contact]);

  useEffect(() => {
    setNotesState(notes);
  }, [notes]);

  async function handleDelete() {
    setIsDeleting(true);
    setError(null);

    try {
      await deleteContactClient(contactId);
      // Redireciona para a lista de contatos após deletar
      router.push("/contatos");
      router.refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao excluir contato");
      setIsDeleting(false);
    }
  }

  async function handleDeleteNote(noteId: string) {
    setDeletingNoteId(noteId);
    setError(null);
    setConfirmDeleteNote(null);

    try {
      await deleteNoteClient(noteId);
      setNotesState((prev) => prev.filter((n) => n.noteId !== noteId));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao excluir nota");
    } finally {
      setDeletingNoteId(null);
    }
  }

  async function handleDeleteRelationship(relationshipId: string) {
    setDeletingRelationshipId(relationshipId);
    setError(null);
    setConfirmDeleteRelationship(null);

    try {
      await deleteRelationshipClient(relationshipId);
      setContactState((prev) => ({
        ...prev,
        relationships: (prev.relationships || []).filter((r) => r.relationshipId !== relationshipId),
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao excluir relacionamento");
    } finally {
      setDeletingRelationshipId(null);
    }
  }

  return (
    <>
      {/* Header Actions */}
      <div className="mb-6 flex justify-end gap-2">
        <Button
          asChild
          variant="glow"
        >
          <Link href={`/contatos/${contactId}/editar`}>
            <Edit className="w-4 h-4 mr-2" />
            Editar
          </Link>
        </Button>
        <Button
          onClick={() => setShowConfirmDialog(true)}
          disabled={isDeleting}
          variant="destructive"
        >
          <Trash2 className="w-4 h-4 mr-2" />
          {isDeleting ? "Excluindo..." : "Excluir"}
        </Button>
      </div>

      {/* Error Message */}
      {error && !showConfirmDialog && (
        <div className="mb-4 glass-card border-destructive/50 bg-destructive/10 p-4">
          <p className="text-sm text-destructive mb-2">{error}</p>
          <Button
            onClick={() => setError(null)}
            variant="ghost"
            size="sm"
          >
            Fechar
          </Button>
        </div>
      )}

      {/* Confirm Delete Note Dialog */}
      <ConfirmDialog
        isOpen={confirmDeleteNote !== null}
        title="Excluir Nota"
        message="Tem certeza que deseja excluir esta nota? Esta ação não pode ser desfeita."
        confirmText="Excluir"
        cancelText="Cancelar"
        onConfirm={() => confirmDeleteNote && handleDeleteNote(confirmDeleteNote)}
        onCancel={() => setConfirmDeleteNote(null)}
        isLoading={deletingNoteId === confirmDeleteNote}
        variant="danger"
      />

      {/* Confirm Delete Relationship Dialog */}
      <ConfirmDialog
        isOpen={confirmDeleteRelationship !== null}
        title="Excluir Relacionamento"
        message="Tem certeza que deseja excluir este relacionamento? Esta ação não pode ser desfeita."
        confirmText="Excluir"
        cancelText="Cancelar"
        onConfirm={() => confirmDeleteRelationship && handleDeleteRelationship(confirmDeleteRelationship)}
        onCancel={() => setConfirmDeleteRelationship(null)}
        isLoading={deletingRelationshipId === confirmDeleteRelationship}
        variant="danger"
      />

      {/* Confirmation Dialog for Contact */}
      <ConfirmDialog
        isOpen={showConfirmDialog}
        title="Excluir Contato"
        message="Tem certeza que deseja excluir este contato? Esta ação não pode ser desfeita."
        confirmText="Excluir"
        cancelText="Cancelar"
        onConfirm={handleDelete}
        onCancel={() => {
          setShowConfirmDialog(false);
          setError(null);
        }}
        isLoading={isDeleting}
        variant="danger"
      />

      {/* Contact Information */}
      <div className="mb-8 glass-card p-6">
        <h2 className="mb-6 text-lg font-semibold">
          Informações do Contato
        </h2>
        <div className="grid gap-6 md:grid-cols-2">
          <div>
            <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
              Nome Completo
            </label>
            <p className="text-sm font-medium">
              {contactState.fullName}
            </p>
          </div>
          {contactState.jobTitle && (
            <div>
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
                Cargo
              </label>
              <p className="text-sm">
                {contactState.jobTitle}
              </p>
            </div>
          )}
          {contactState.company && (
            <div>
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
                <Building2 className="w-3 h-3" />
                Empresa
              </label>
              <p className="text-sm">
                {contactState.company}
              </p>
            </div>
          )}
          {contactState.emails.length > 0 && (
            <div>
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
                <Mail className="w-3 h-3" />
                E-mails
              </label>
              <ul className="space-y-1">
                {contactState.emails.map((email, index) => (
                  <li key={index} className="text-sm">
                    <a
                      href={`mailto:${email}`}
                      className="text-primary hover:underline flex items-center gap-1"
                    >
                      {email}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}
          {contactState.phones.length > 0 && (
            <div>
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
                <Phone className="w-3 h-3" />
                Telefones
              </label>
              <ul className="space-y-1">
                {contactState.phones.map((phone, index) => (
                  <li key={index} className="text-sm">
                    <a
                      href={`tel:${phone}`}
                      className="text-primary hover:underline flex items-center gap-1"
                    >
                      {phone}
                    </a>
                  </li>
                ))}
              </ul>
            </div>
          )}
          {contactState.address && (
            <div className="md:col-span-2">
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
                <MapPin className="w-3 h-3" />
                Endereço
              </label>
              <p className="text-sm">
                {formatAddress(contactState.address)}
              </p>
            </div>
          )}
          {contactState.tags.length > 0 && (
            <div className="md:col-span-2">
              <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
                <Tag className="w-3 h-3" />
                Tags
              </label>
              <div className="flex flex-wrap gap-2">
                {contactState.tags.map((tag, index) => (
                  <span
                    key={index}
                    className="rounded-full bg-primary/10 text-primary px-3 py-1 text-xs font-medium"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          )}
          <div>
            <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
              <Calendar className="w-3 h-3" />
              Criado em
            </label>
            <p className="text-sm">
              {formatDate(contactState.createdAt)}
            </p>
          </div>
          <div>
            <label className="text-xs font-medium text-muted-foreground flex items-center gap-1 mb-2">
              <Calendar className="w-3 h-3" />
              Atualizado em
            </label>
            <p className="text-sm">
              {formatDate(contactState.updatedAt)}
            </p>
          </div>
        </div>
      </div>

      {/* Relationships */}
      <div className="mb-8 glass-card p-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <LinkIcon className="w-5 h-5" />
            Relacionamentos
          </h2>
          <Button asChild variant="glow" size="sm">
            <Link href={`/contatos/${contactId}/relacionamentos/novo`}>
              <Plus className="w-4 h-4 mr-2" />
              Adicionar Relacionamento
            </Link>
          </Button>
        </div>
        {contactState.relationships.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Nenhum relacionamento cadastrado.
          </p>
        ) : (
          <div className="space-y-3">
            {contactState.relationships.map((relationship: Relationship) => (
              <div
                key={relationship.relationshipId}
                className="glass-card border-border/50 p-4"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="rounded-full bg-primary/10 text-primary px-3 py-1 text-xs font-medium">
                        {relationship.type}
                      </span>
                      {relationship.isConfirmed && (
                        <span className="rounded-full bg-success/10 text-success px-3 py-1 text-xs font-medium">
                          Confirmado
                        </span>
                      )}
                      {relationship.strength > 0 && (
                        <span className="text-xs text-muted-foreground">
                          Força: {Math.round(relationship.strength * 100)}%
                        </span>
                      )}
                    </div>
                    {relationship.description && (
                      <p className="mt-2 text-sm">
                        {relationship.description}
                      </p>
                    )}
                    <Link
                      href={`/contatos/${relationship.targetContactId}`}
                      className="mt-2 text-sm text-primary hover:underline inline-flex items-center gap-1"
                    >
                      Ver contato relacionado →
                    </Link>
                  </div>
                  <Button
                    onClick={() => setConfirmDeleteRelationship(relationship.relationshipId)}
                    disabled={deletingRelationshipId === relationship.relationshipId}
                    variant="destructive"
                    size="sm"
                    title="Excluir relacionamento"
                  >
                    {deletingRelationshipId === relationship.relationshipId ? "Excluindo..." : "Excluir"}
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

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

      {/* Notes */}
      <div className="glass-card p-6">
        <div className="mb-4 flex items-center justify-between flex-wrap gap-2">
          <h2 className="text-lg font-semibold">
            Notas
          </h2>
          <div className="flex gap-2">
            <Button asChild variant="glow" size="sm">
              <Link href={`/contatos/${contactId}?novo=true`}>
                <Plus className="w-4 h-4 mr-2" />
                Adicionar Nota
              </Link>
            </Button>
            <Button asChild variant="ghost" size="sm">
              <Link href={`/contatos/${contactId}?audio=true`}>
                <Mic className="w-4 h-4 mr-2" />
                Nota de Áudio
              </Link>
            </Button>
          </div>
        </div>
        {notes.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            Nenhuma nota cadastrada.
          </p>
        ) : (
          <div className="space-y-4">
            {notesState.map((note) => (
              <div
                key={note.noteId}
                className="glass-card border-border/50 p-4"
              >
                <div className="mb-2 flex items-center justify-between flex-wrap gap-2">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="rounded-full bg-accent/10 text-accent px-3 py-1 text-xs font-medium">
                      {getNoteTypeLabel(note.type)}
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
                    {deletingNoteId === note.noteId ? "Excluindo..." : "Excluir"}
                  </Button>
                </div>
                {note.type === NoteType.Audio && (
                  <AudioPlayer noteId={note.noteId} />
                )}
                {note.type === NoteType.Audio && note.rawContent && (
                  <div className="mb-3 glass-card border-primary/20 bg-primary/5 p-3">
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
                {note.type === NoteType.Text && (
                  <p className="whitespace-pre-wrap text-sm">
                    {note.rawContent}
                  </p>
                )}
                {/* Show TTS button for audio notes with structured data */}
                {note.type === NoteType.Audio && note.structuredData && (
                  <div className="mt-2 mb-2">
                    <StructuredDataTTSPlayer structuredData={note.structuredData} />
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
    </>
  );
}

function AudioPlayer({ noteId }: { noteId: string }) {
  const [isPlaying, setIsPlaying] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [playbackRate, setPlaybackRate] = useState(1.0);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animationFrameRef = useRef<number | null>(null);
  const audioContextRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const [audioBlobUrl, setAudioBlobUrl] = useState<string | null>(null);

  async function loadAudio() {
    if (audioBlobUrl) return audioBlobUrl;

    setIsLoading(true);
    setError(null);

    try {
      const baseUrl = getApiBaseUrl();
      const url = `${baseUrl}/api/notes/${noteId}/audio`;

      const response = await fetch(url, {
        credentials: "include",
      });

      if (!response.ok) {
        throw new Error("Erro ao carregar áudio");
      }

      const blob = await response.blob();
      const blobUrl = URL.createObjectURL(blob);
      setAudioBlobUrl(blobUrl);
      return blobUrl;
    } catch (err) {
      setError("Erro ao carregar áudio");
      throw err;
    } finally {
      setIsLoading(false);
    }
  }

  // Setup Web Audio API for visualization
  useEffect(() => {
    if (!audioRef.current || !canvasRef.current || !audioBlobUrl) return;

    const audio = audioRef.current;
    const canvas = canvasRef.current;
    
    // Set canvas size
    const resizeCanvas = () => {
      const rect = canvas.parentElement?.getBoundingClientRect();
      if (rect) {
        canvas.width = rect.width;
        canvas.height = 64;
      }
    };
    
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);
    
    const setupAudioContext = async () => {
      try {
        // Reuse existing context if available, otherwise create new one
        let audioContext = audioContextRef.current;
        
        if (!audioContext) {
          audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        }
        
        // Resume AudioContext if suspended (required on Android)
        if (audioContext.state === 'suspended') {
          await audioContext.resume();
        }
        
        // Only create new analyser and source if they don't exist
        if (!analyserRef.current) {
          const analyser = audioContext.createAnalyser();
          analyser.fftSize = 256;
          
          // Check if audio element already has a source connected
          // If not, create a new source
          try {
            const source = audioContext.createMediaElementSource(audio);
            source.connect(analyser);
            analyser.connect(audioContext.destination);
            analyserRef.current = analyser;
          } catch (err: any) {
            // If createMediaElementSource fails (e.g., not supported on Android, already connected), 
            // skip visualization but don't block audio playback
            console.warn('Audio visualization not available:', err.message || err.name);
            analyserRef.current = null; // Clear analyser so visualization is skipped
          }
        }
        
        audioContextRef.current = audioContext;
      } catch (err: any) {
        console.warn('Error setting up audio context for visualization:', err.message || err);
        // Don't block audio playback if visualization fails
        audioContextRef.current = null;
        analyserRef.current = null;
      }
    };

    // Only setup AudioContext if audio has a source
    // But don't wait for it - audio playback should work independently
    if (audio.src) {
      setupAudioContext().catch(err => {
        console.warn('Failed to setup audio context:', err);
      });
    }

    return () => {
      window.removeEventListener('resize', resizeCanvas);
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [audioBlobUrl]);

  // Update current time
  useEffect(() => {
    if (!audioRef.current) return;

    const audio = audioRef.current;
    
    const updateTime = () => {
      setCurrentTime(audio.currentTime);
      if (audio.duration) {
        setDuration(audio.duration);
      }
    };

    const handleTimeUpdate = () => updateTime();
    const handleLoadedMetadata = () => {
      setDuration(audio.duration);
    };

    audio.addEventListener('timeupdate', handleTimeUpdate);
    audio.addEventListener('loadedmetadata', handleLoadedMetadata);

    return () => {
      audio.removeEventListener('timeupdate', handleTimeUpdate);
      audio.removeEventListener('loadedmetadata', handleLoadedMetadata);
    };
  }, [audioBlobUrl]);

  // Draw waveform visualization
  useEffect(() => {
    if (!canvasRef.current || !analyserRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const analyser = analyserRef.current;
    const bufferLength = analyser.frequencyBinCount;
    const dataArray = new Uint8Array(bufferLength);

    const draw = () => {
      if (!isPlaying) {
        // Draw static waveform when paused
        ctx.fillStyle = 'rgba(0, 0, 0, 0)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        return;
      }

      analyser.getByteFrequencyData(dataArray);

      ctx.fillStyle = 'rgba(0, 0, 0, 0)';
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      const barWidth = (canvas.width / bufferLength) * 2.5;
      let barHeight;
      let x = 0;

      for (let i = 0; i < bufferLength; i++) {
        barHeight = (dataArray[i] / 255) * canvas.height * 0.8;

        const gradient = ctx.createLinearGradient(0, canvas.height - barHeight, 0, canvas.height);
        gradient.addColorStop(0, 'rgba(147, 51, 234, 0.9)'); // purple-600
        gradient.addColorStop(1, 'rgba(147, 51, 234, 0.2)');

        ctx.fillStyle = gradient;
        ctx.fillRect(x, canvas.height - barHeight, barWidth, barHeight);

        x += barWidth + 1;
      }

      if (isPlaying) {
        animationFrameRef.current = requestAnimationFrame(draw);
      }
    };

    if (isPlaying) {
      draw();
    } else {
      // Clear canvas when paused
      ctx.clearRect(0, 0, canvas.width, canvas.height);
    }

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [isPlaying]);

  async function handlePlay() {
    if (!audioRef.current) return;

    if (isPlaying) {
      audioRef.current.pause();
      setIsPlaying(false);
      return;
    }

    try {
      const url = await loadAudio();
      if (audioRef.current && url) {
        // Try to resume AudioContext if it exists (for visualization)
        // But don't block playback if it fails
        if (audioContextRef.current && audioContextRef.current.state === 'suspended') {
          audioContextRef.current.resume().catch(err => {
            console.warn('Failed to resume AudioContext:', err);
          });
        }
        
        audioRef.current.src = url;
        audioRef.current.playbackRate = playbackRate;
        
        // Play audio - this should work independently of AudioContext
        const playPromise = audioRef.current.play();
        if (playPromise !== undefined) {
          await playPromise;
        }
        
        setIsPlaying(true);
      }
    } catch (err: any) {
      console.error('Error playing audio:', err);
      // Provide more specific error messages
      let errorMessage = "Erro ao reproduzir áudio";
      if (err.message) {
        errorMessage = err.message;
      } else if (err.name === 'NotSupportedError' || err.name === 'NotAllowedError') {
        errorMessage = "Reprodução de áudio não suportada. Verifique as permissões do navegador.";
      }
      setError(errorMessage);
      setIsPlaying(false);
    }
  }

  function handlePlaybackRateChange(newRate: number) {
    setPlaybackRate(newRate);
    if (audioRef.current) {
      audioRef.current.playbackRate = newRate;
    }
  }

  function handleSeek(e: React.MouseEvent<HTMLDivElement>) {
    if (!audioRef.current || !duration) return;

    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const percentage = x / rect.width;
    const newTime = percentage * duration;

    audioRef.current.currentTime = newTime;
    setCurrentTime(newTime);
  }

  function handleEnded() {
    setIsPlaying(false);
  }

  function handlePause() {
    setIsPlaying(false);
  }

  // Limpar blob URL ao desmontar
  useEffect(() => {
    return () => {
      if (audioBlobUrl) {
        URL.revokeObjectURL(audioBlobUrl);
      }
    };
  }, [audioBlobUrl]);

  return (
    <div className="mb-3 glass-card border-border/50 p-3">
      <div className="flex items-center gap-3 mb-2">
        <button
          onClick={handlePlay}
          disabled={isLoading}
          className="flex h-10 w-10 items-center justify-center rounded-full bg-accent text-accent-foreground hover:bg-accent/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
          aria-label={isPlaying ? "Pausar áudio" : "Reproduzir áudio"}
        >
          {isLoading ? (
            <svg
              className="h-5 w-5 animate-spin"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              ></circle>
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              ></path>
            </svg>
          ) : isPlaying ? (
            <svg
              className="h-5 w-5"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zM7 8a1 1 0 012 0v4a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v4a1 1 0 102 0V8a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          ) : (
            <svg
              className="h-5 w-5"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z"
                clipRule="evenodd"
              />
            </svg>
          )}
        </button>
        <div className="flex-1 min-w-0">
          {error ? (
            <p className="text-xs text-destructive">{error}</p>
          ) : (
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted-foreground font-mono">
                {formatTime(currentTime)} / {formatTime(duration)}
              </span>
            </div>
          )}
        </div>
        <PlaybackRateControl
          playbackRate={playbackRate}
          onChange={handlePlaybackRateChange}
        />
      </div>
      
      {/* Audio Spectrum Visualization */}
      {audioBlobUrl && (
        <div className="mb-2">
          <canvas
            ref={canvasRef}
            className="w-full h-16 rounded-lg bg-secondary/50"
          />
        </div>
      )}

      {/* Progress Bar */}
      {duration > 0 && (
        <div className="relative">
          <div
            className="h-2 bg-secondary/50 rounded-full cursor-pointer"
            onClick={handleSeek}
          >
            <div
              className="h-full bg-accent rounded-full transition-all duration-100"
              style={{ width: `${(currentTime / duration) * 100}%` }}
            />
          </div>
        </div>
      )}
      
      <audio
        ref={audioRef}
        onEnded={handleEnded}
        onPause={handlePause}
        preload="none"
      />
    </div>
  );
}

function PlaybackRateControl({
  playbackRate,
  onChange,
}: {
  playbackRate: number;
  onChange: (rate: number) => void;
}) {
  const rates = [0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0];
  const [isOpen, setIsOpen] = useState(false);

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-1 rounded-lg border border-border bg-secondary/50 px-2 py-1 text-xs font-medium hover:bg-secondary transition-colors"
        title="Velocidade de reprodução"
      >
        <svg
          className="h-3 w-3"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M13 10V3L4 14h7v7l9-11h-7z"
          />
        </svg>
        <span>{playbackRate}x</span>
      </button>
      {isOpen && (
        <>
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
          />
          <div className="absolute right-0 top-full mt-1 z-20 rounded-lg glass-card border border-border shadow-lg py-1 min-w-[120px]">
            {rates.map((rate) => (
              <button
                key={rate}
                onClick={() => {
                  onChange(rate);
                  setIsOpen(false);
                }}
                className={`w-full px-3 py-1.5 text-left text-xs hover:bg-secondary/50 transition-colors ${
                  playbackRate === rate
                    ? "bg-accent/10 text-accent font-medium"
                    : "text-foreground"
                }`}
              >
                {rate}x
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

function StructuredDataTTSPlayer({ structuredData }: { structuredData: string }) {
  const [isPlaying, setIsPlaying] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [playbackRate, setPlaybackRate] = useState(1.0);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const animationFrameRef = useRef<number | null>(null);
  const audioContextRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const [audioBlobUrl, setAudioBlobUrl] = useState<string | null>(null);
  const synthesisRef = useRef<SpeechSynthesisUtterance | null>(null);

  // Parse structured data
  const [parsedData, setParsedData] = useState<{
    responseMediaId?: string | null;
    summary?: string | null;
    tasks?: Array<{ description: string; dueDate?: string | null; priority?: string | null }>;
  } | null>(null);

  useEffect(() => {
    try {
      const data = JSON.parse(structuredData);
      setParsedData({
        responseMediaId: data?.responseMediaId || null,
        summary: data?.summary || null,
        tasks: data?.tasks || [],
      });
    } catch (e) {
      console.error('StructuredDataTTSPlayer - Error parsing JSON:', e);
      setParsedData(null);
    }
  }, [structuredData]);

  // Build text to speak: summary + tasks
  const buildTextToSpeak = (): string => {
    if (!parsedData) return '';
    
    const parts: string[] = [];
    
    if (parsedData.summary) {
      parts.push(`Resumo: ${parsedData.summary}`);
    }
    
    if (parsedData.tasks && parsedData.tasks.length > 0) {
      parts.push(`Tarefas:`);
      parsedData.tasks.forEach((task, index) => {
        const taskText = `${index + 1}. ${task.description}`;
        if (task.priority) {
          parts.push(`${taskText}. Prioridade: ${task.priority}`);
        } else {
          parts.push(taskText);
        }
      });
    }
    
    return parts.join('. ');
  };

  const textToSpeak = buildTextToSpeak();

  // Setup Web Audio API for visualization (only for pre-generated audio)
  useEffect(() => {
    if (!audioRef.current || !canvasRef.current || !audioBlobUrl || !parsedData?.responseMediaId) return;

    const audio = audioRef.current;
    const canvas = canvasRef.current;
    
    const resizeCanvas = () => {
      const rect = canvas.parentElement?.getBoundingClientRect();
      if (rect) {
        canvas.width = rect.width;
        canvas.height = 48;
      }
    };
    
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);
    
    const setupAudioContext = async () => {
      try {
        // Reuse existing context if available, otherwise create new one
        let audioContext = audioContextRef.current;
        
        if (!audioContext) {
          audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        }
        
        // Resume AudioContext if suspended (required on Android)
        if (audioContext.state === 'suspended') {
          await audioContext.resume();
        }
        
        // Only create new analyser and source if they don't exist
        if (!analyserRef.current) {
          const analyser = audioContext.createAnalyser();
          analyser.fftSize = 256;
          
          // Check if audio element already has a source connected
          // If not, create a new source
          try {
            const source = audioContext.createMediaElementSource(audio);
            source.connect(analyser);
            analyser.connect(audioContext.destination);
            analyserRef.current = analyser;
          } catch (err: any) {
            // If createMediaElementSource fails (e.g., not supported on Android, already connected), 
            // skip visualization but don't block audio playback
            console.warn('Audio visualization not available:', err.message || err.name);
            analyserRef.current = null; // Clear analyser so visualization is skipped
          }
        }
        
        audioContextRef.current = audioContext;
      } catch (err: any) {
        console.warn('Error setting up audio context for visualization:', err.message || err);
        // Don't block audio playback if visualization fails
        audioContextRef.current = null;
        analyserRef.current = null;
      }
    };

    // Only setup AudioContext if audio has a source
    // But don't wait for it - audio playback should work independently
    if (audio.src) {
      setupAudioContext().catch(err => {
        console.warn('Failed to setup audio context:', err);
      });
    }

    return () => {
      window.removeEventListener('resize', resizeCanvas);
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [audioBlobUrl, parsedData?.responseMediaId]);

  // Update current time (only for pre-generated audio)
  useEffect(() => {
    if (!audioRef.current || !parsedData?.responseMediaId) return;

    const audio = audioRef.current;
    
    const updateTime = () => {
      setCurrentTime(audio.currentTime);
      if (audio.duration) {
        setDuration(audio.duration);
      }
    };

    const handleTimeUpdate = () => updateTime();
    const handleLoadedMetadata = () => {
      setDuration(audio.duration);
    };

    audio.addEventListener('timeupdate', handleTimeUpdate);
    audio.addEventListener('loadedmetadata', handleLoadedMetadata);

    return () => {
      audio.removeEventListener('timeupdate', handleTimeUpdate);
      audio.removeEventListener('loadedmetadata', handleLoadedMetadata);
    };
  }, [audioBlobUrl, parsedData?.responseMediaId]);

  // Draw waveform visualization (only for pre-generated audio)
  useEffect(() => {
    if (!canvasRef.current || !analyserRef.current || !parsedData?.responseMediaId) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const analyser = analyserRef.current;
    const bufferLength = analyser.frequencyBinCount;
    const dataArray = new Uint8Array(bufferLength);

    const draw = () => {
      if (!isPlaying) {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        return;
      }

      analyser.getByteFrequencyData(dataArray);
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      const barWidth = (canvas.width / bufferLength) * 2.5;
      let barHeight;
      let x = 0;

      for (let i = 0; i < bufferLength; i++) {
        barHeight = (dataArray[i] / 255) * canvas.height * 0.8;

        const gradient = ctx.createLinearGradient(0, canvas.height - barHeight, 0, canvas.height);
        gradient.addColorStop(0, 'rgba(147, 51, 234, 0.9)'); // purple-600
        gradient.addColorStop(1, 'rgba(147, 51, 234, 0.2)');

        ctx.fillStyle = gradient;
        ctx.fillRect(x, canvas.height - barHeight, barWidth, barHeight);

        x += barWidth + 1;
      }

      if (isPlaying) {
        animationFrameRef.current = requestAnimationFrame(draw);
      }
    };

    if (isPlaying) {
      draw();
    } else {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
    }

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
    };
  }, [isPlaying, parsedData?.responseMediaId]);

  // Cleanup
  useEffect(() => {
    return () => {
      if (audioBlobUrl) {
        URL.revokeObjectURL(audioBlobUrl);
      }
      if (synthesisRef.current) {
        window.speechSynthesis.cancel();
      }
    };
  }, [audioBlobUrl]);

  async function loadAudioFromMediaId(mediaId: string) {
    if (audioBlobUrl) return audioBlobUrl;

    setIsLoading(true);
    setError(null);

    try {
      const baseUrl = getApiBaseUrl();
      const url = `${baseUrl}/api/media/${mediaId}/file`;

      const response = await fetch(url, {
        credentials: "include",
      });

      if (!response.ok) {
        throw new Error("Erro ao carregar áudio");
      }

      const blob = await response.blob();
      const blobUrl = URL.createObjectURL(blob);
      setAudioBlobUrl(blobUrl);
      return blobUrl;
    } catch (err) {
      setError("Erro ao carregar áudio");
      throw err;
    } finally {
      setIsLoading(false);
    }
  }

  function speakWithWebSpeechAPI() {
    if (!('speechSynthesis' in window)) {
      setError("Seu navegador não suporta síntese de voz");
      return;
    }

    // Cancel any ongoing speech
    window.speechSynthesis.cancel();

    const utterance = new SpeechSynthesisUtterance(textToSpeak);
    utterance.lang = 'pt-BR';
    utterance.rate = playbackRate; // Use the playback rate
    utterance.pitch = 1.0;
    utterance.volume = 1.0;

    utterance.onstart = () => {
      setIsPlaying(true);
      setError(null);
    };

    utterance.onend = () => {
      setIsPlaying(false);
      synthesisRef.current = null;
    };

    utterance.onerror = (e) => {
      setError("Erro ao reproduzir áudio");
      setIsPlaying(false);
      synthesisRef.current = null;
    };

    synthesisRef.current = utterance;
    window.speechSynthesis.speak(utterance);
  }

  async function handlePlay() {
    if (isPlaying) {
      // Stop playing
      if (parsedData?.responseMediaId && audioRef.current) {
        audioRef.current.pause();
        audioRef.current.currentTime = 0;
      } else if (synthesisRef.current) {
        window.speechSynthesis.cancel();
      }
      setIsPlaying(false);
      return;
    }

    setError(null);

    // If we have a pre-generated audio, use it
    if (parsedData?.responseMediaId) {
      try {
        // Try to resume AudioContext if it exists (for visualization)
        // But don't block playback if it fails
        if (audioContextRef.current && audioContextRef.current.state === 'suspended') {
          audioContextRef.current.resume().catch(err => {
            console.warn('Failed to resume AudioContext:', err);
          });
        }
        
        const url = await loadAudioFromMediaId(parsedData.responseMediaId);
        if (audioRef.current && url) {
          audioRef.current.src = url;
          audioRef.current.playbackRate = playbackRate;
          
          // Play audio - this should work independently of AudioContext
          const playPromise = audioRef.current.play();
          if (playPromise !== undefined) {
            await playPromise;
          }
          
          setIsPlaying(true);
        }
      } catch (err: any) {
        console.error('Error playing audio:', err);
        // Provide more specific error messages
        let errorMessage = "Erro ao reproduzir áudio";
        if (err.message) {
          errorMessage = err.message;
        } else if (err.name === 'NotSupportedError' || err.name === 'NotAllowedError') {
          errorMessage = "Reprodução de áudio não suportada. Verifique as permissões do navegador.";
        }
        setError(errorMessage);
        setIsPlaying(false);
      }
    } else {
      // Use Web Speech API as fallback
      speakWithWebSpeechAPI();
    }
  }

  function handlePlaybackRateChange(newRate: number) {
    setPlaybackRate(newRate);
    if (audioRef.current) {
      audioRef.current.playbackRate = newRate;
    }
    // For Web Speech API, we need to update the rate if currently playing
    if (synthesisRef.current && isPlaying && !parsedData?.responseMediaId) {
      window.speechSynthesis.cancel();
      speakWithWebSpeechAPI();
    }
  }

  function handleEnded() {
    setIsPlaying(false);
  }

  function handlePause() {
    setIsPlaying(false);
  }

  // Always show button if we have structuredData (even if empty, user can try to play)
  // The button will work if we have responseMediaId OR if we have text to speak
  const hasContent = textToSpeak.trim().length > 0 || parsedData?.responseMediaId !== null;
  
  if (!hasContent || !parsedData) {
    return null;
  }

  return (
    <div className="mb-2 w-full">
      <div className="flex items-center gap-2 mb-2">
        <button
          onClick={handlePlay}
          disabled={isLoading}
          className="flex h-8 w-8 items-center justify-center rounded-full bg-accent text-accent-foreground hover:bg-accent/90 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
          aria-label={isPlaying ? "Pausar áudio" : "Reproduzir resumo e tarefas em áudio"}
          title={isPlaying ? "Pausar áudio" : "Reproduzir resumo e tarefas em áudio"}
        >
          {isLoading ? (
            <svg
              className="h-4 w-4 animate-spin"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
            >
              <circle
                className="opacity-25"
                cx="12"
                cy="12"
                r="10"
                stroke="currentColor"
                strokeWidth="4"
              ></circle>
              <path
                className="opacity-75"
                fill="currentColor"
                d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
              ></path>
            </svg>
          ) : isPlaying ? (
            <svg
              className="h-4 w-4"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zM7 8a1 1 0 012 0v4a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v4a1 1 0 102 0V8a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          ) : (
            <svg
              className="h-4 w-4"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM9.555 7.168A1 1 0 008 8v4a1 1 0 001.555.832l3-2a1 1 0 000-1.664l-3-2z"
                clipRule="evenodd"
              />
            </svg>
          )}
        </button>
        <div className="flex-1 min-w-0">
          {error ? (
            <span className="text-xs text-destructive">{error}</span>
          ) : parsedData?.responseMediaId ? (
            <span className="text-xs text-muted-foreground font-mono">
              {formatTime(currentTime)} / {formatTime(duration)}
            </span>
          ) : (
            <span className="text-xs text-muted-foreground">
              {isPlaying ? "Reproduzindo..." : "Reproduzir resumo e tarefas"}
            </span>
          )}
        </div>
        <PlaybackRateControl
          playbackRate={playbackRate}
          onChange={handlePlaybackRateChange}
        />
      </div>
      
      {/* Audio Spectrum Visualization (only for pre-generated audio) */}
      {parsedData?.responseMediaId && audioBlobUrl && (
        <div className="mb-2">
          <canvas
            ref={canvasRef}
            className="w-full h-12 rounded-lg bg-secondary/50"
          />
        </div>
      )}

      {/* Progress Bar (only for pre-generated audio) */}
      {parsedData?.responseMediaId && duration > 0 && (
        <div className="relative">
          <div
            className="h-1.5 bg-secondary/50 rounded-full cursor-pointer"
            onClick={(e) => {
              if (!audioRef.current || !duration || !parsedData?.responseMediaId) return;
              const rect = e.currentTarget.getBoundingClientRect();
              const x = e.clientX - rect.left;
              const percentage = x / rect.width;
              const newTime = percentage * duration;
              audioRef.current.currentTime = newTime;
              setCurrentTime(newTime);
            }}
          >
            <div
              className="h-full bg-accent rounded-full transition-all duration-100"
              style={{ width: `${(currentTime / duration) * 100}%` }}
            />
          </div>
        </div>
      )}
      
      <audio
        ref={audioRef}
        onEnded={handleEnded}
        onPause={handlePause}
        preload="none"
      />
    </div>
  );
}
