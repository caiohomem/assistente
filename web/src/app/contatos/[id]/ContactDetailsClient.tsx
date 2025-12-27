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
        <Link
          href={`/contatos/${contactId}/editar`}
          className="rounded-md bg-zinc-900 dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700 transition-colors"
        >
          Editar
        </Link>
        <button
          onClick={() => setShowConfirmDialog(true)}
          disabled={isDeleting}
          className="rounded-md bg-red-600 dark:bg-red-700 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 dark:hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isDeleting ? "Excluindo..." : "Excluir"}
        </button>
      </div>

      {/* Error Message */}
      {error && !showConfirmDialog && (
        <div className="mb-4 rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-3">
          <p className="text-sm text-red-700 dark:text-red-400">{error}</p>
          <button
            onClick={() => setError(null)}
            className="mt-2 text-xs text-red-600 dark:text-red-400 hover:underline"
          >
            Fechar
          </button>
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
      <div className="mb-8 rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 p-6">
        <h2 className="mb-4 text-lg font-semibold text-zinc-900 dark:text-zinc-100">
          Informações do Contato
        </h2>
        <div className="grid gap-4 md:grid-cols-2">
          <div>
            <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
              Nome Completo
            </label>
            <p className="mt-1 text-sm text-zinc-900 dark:text-zinc-100">
              {contactState.fullName}
            </p>
          </div>
          {contactState.jobTitle && (
            <div>
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                Cargo
              </label>
              <p className="mt-1 text-sm text-zinc-900 dark:text-zinc-100">
                {contactState.jobTitle}
              </p>
            </div>
          )}
          {contactState.company && (
            <div>
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                Empresa
              </label>
              <p className="mt-1 text-sm text-zinc-900 dark:text-zinc-100">
                {contactState.company}
              </p>
            </div>
          )}
          {contactState.emails.length > 0 && (
            <div>
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                E-mails
              </label>
              <ul className="mt-1 space-y-1">
                {contactState.emails.map((email, index) => (
                  <li key={index} className="text-sm">
                    <a
                      href={`mailto:${email}`}
                      className="text-blue-600 dark:text-blue-400 hover:underline"
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
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                Telefones
              </label>
              <ul className="mt-1 space-y-1">
                {contactState.phones.map((phone, index) => (
                  <li key={index} className="text-sm">
                    <a
                      href={`tel:${phone}`}
                      className="text-blue-600 dark:text-blue-400 hover:underline"
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
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                Endereço
              </label>
              <p className="mt-1 text-sm text-zinc-900 dark:text-zinc-100">
                {formatAddress(contactState.address)}
              </p>
            </div>
          )}
          {contactState.tags.length > 0 && (
            <div className="md:col-span-2">
              <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
                Tags
              </label>
              <div className="mt-1 flex flex-wrap gap-2">
                {contactState.tags.map((tag, index) => (
                  <span
                    key={index}
                    className="rounded-full bg-zinc-100 dark:bg-zinc-700 px-3 py-1 text-xs font-medium text-zinc-700 dark:text-zinc-300"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          )}
          <div>
            <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
              Criado em
            </label>
            <p className="mt-1 text-sm text-zinc-900 dark:text-zinc-100">
              {formatDate(contactState.createdAt)}
            </p>
          </div>
          <div>
            <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400">
              Atualizado em
            </label>
            <p className="mt-1 text-sm text-zinc-900 dark:text-zinc-100">
              {formatDate(contactState.updatedAt)}
            </p>
          </div>
        </div>
      </div>

      {/* Relationships */}
      <div className="mb-8 rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 p-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
            Relacionamentos
          </h2>
          <Link
            href={`/contatos/${contactId}/relacionamentos/novo`}
            className="rounded-md bg-zinc-900 dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700"
          >
            Adicionar Relacionamento
          </Link>
        </div>
        {contactState.relationships.length === 0 ? (
          <p className="text-sm text-zinc-500 dark:text-zinc-400">
            Nenhum relacionamento cadastrado.
          </p>
        ) : (
          <div className="space-y-3">
            {contactState.relationships.map((relationship: Relationship) => (
              <div
                key={relationship.relationshipId}
                className="rounded-md border border-zinc-100 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-700 p-4"
              >
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="rounded-full bg-blue-100 dark:bg-blue-900/30 px-3 py-1 text-xs font-medium text-blue-700 dark:text-blue-400">
                        {relationship.type}
                      </span>
                      {relationship.isConfirmed && (
                        <span className="rounded-full bg-green-100 dark:bg-green-900/30 px-3 py-1 text-xs font-medium text-green-700 dark:text-green-400">
                          Confirmado
                        </span>
                      )}
                      {relationship.strength > 0 && (
                        <span className="text-xs text-zinc-500 dark:text-zinc-400">
                          Força: {Math.round(relationship.strength * 100)}%
                        </span>
                      )}
                    </div>
                    {relationship.description && (
                      <p className="mt-2 text-sm text-zinc-700 dark:text-zinc-300">
                        {relationship.description}
                      </p>
                    )}
                    <Link
                      href={`/contatos/${relationship.targetContactId}`}
                      className="mt-2 text-sm text-blue-600 dark:text-blue-400 hover:underline"
                    >
                      Ver contato relacionado →
                    </Link>
                  </div>
                  <button
                    onClick={() => setConfirmDeleteRelationship(relationship.relationshipId)}
                    disabled={deletingRelationshipId === relationship.relationshipId}
                    className="ml-4 rounded-md bg-red-600 dark:bg-red-700 px-3 py-1 text-xs font-medium text-white hover:bg-red-700 dark:hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Excluir relacionamento"
                  >
                    {deletingRelationshipId === relationship.relationshipId ? "Excluindo..." : "Excluir"}
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Notes */}
      <div className="rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 p-6">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
            Notas
          </h2>
          <div className="flex gap-2">
            <Link
              href={`/contatos/${contactId}/notas/novo`}
              className="rounded-md bg-zinc-900 dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700"
            >
              Adicionar Nota
            </Link>
            <Link
              href={`/contatos/${contactId}/notas-audio`}
              className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700"
            >
              Nota de Áudio
            </Link>
          </div>
        </div>
        {notes.length === 0 ? (
          <p className="text-sm text-zinc-500 dark:text-zinc-400">
            Nenhuma nota cadastrada.
          </p>
        ) : (
          <div className="space-y-4">
            {notesState.map((note) => (
              <div
                key={note.noteId}
                className="rounded-md border border-zinc-100 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-700 p-4"
              >
                <div className="mb-2 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="rounded-full bg-purple-100 dark:bg-purple-900/30 px-3 py-1 text-xs font-medium text-purple-700 dark:text-purple-400">
                      {getNoteTypeLabel(note.type)}
                    </span>
                    <span className="text-xs text-zinc-500 dark:text-zinc-400">
                      {formatDate(note.createdAt)}
                    </span>
                  </div>
                  <button
                    onClick={() => setConfirmDeleteNote(note.noteId)}
                    disabled={deletingNoteId === note.noteId}
                    className="rounded-md bg-red-600 dark:bg-red-700 px-3 py-1 text-xs font-medium text-white hover:bg-red-700 dark:hover:bg-red-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                    title="Excluir nota"
                  >
                    {deletingNoteId === note.noteId ? "Excluindo..." : "Excluir"}
                  </button>
                </div>
                {note.type === NoteType.Audio && (
                  <AudioPlayer noteId={note.noteId} />
                )}
                {note.type === NoteType.Audio && note.rawContent && (
                  <div className="mb-3 rounded-md border border-blue-200 dark:border-blue-800 bg-blue-50 dark:bg-blue-900/20 p-3">
                    <div className="mb-2 flex items-center gap-2">
                      <svg
                        className="h-4 w-4 text-blue-600 dark:text-blue-400"
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M9 19V6l12-3v13M9 19c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zm12-3c0 1.105-1.343 2-3 2s-3-.895-3-2 1.343-2 3-2 3 .895 3 2zM9 10l12-3"
                        />
                      </svg>
                      <span className="text-xs font-medium text-blue-700 dark:text-blue-300">
                        Transcrição de Áudio
                      </span>
                    </div>
                    <p className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-300 leading-relaxed">
                      {note.rawContent}
                    </p>
                  </div>
                )}
                {note.type === NoteType.Text && (
                  <p className="whitespace-pre-wrap text-sm text-zinc-700 dark:text-zinc-300">
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
                    <summary className="cursor-pointer text-xs text-zinc-500 dark:text-zinc-400 hover:text-zinc-700 dark:hover:text-zinc-300">
                      Ver dados estruturados
                    </summary>
                    <div className="mt-2">
                      <pre className="overflow-auto rounded bg-zinc-100 dark:bg-zinc-800 p-2 text-xs text-zinc-900 dark:text-zinc-100">
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
        const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        const analyser = audioContext.createAnalyser();
        const source = audioContext.createMediaElementSource(audio);
        
        analyser.fftSize = 256;
        source.connect(analyser);
        analyser.connect(audioContext.destination);
        
        audioContextRef.current = audioContext;
        analyserRef.current = analyser;
      } catch (err) {
        console.error('Error setting up audio context:', err);
      }
    };

    if (audio.src) {
      setupAudioContext();
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
        audioRef.current.src = url;
        audioRef.current.playbackRate = playbackRate;
        await audioRef.current.play();
        setIsPlaying(true);
      }
    } catch (err) {
      setError("Erro ao reproduzir áudio");
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
    <div className="mb-3 rounded-md border border-zinc-200 dark:border-zinc-600 bg-zinc-100 dark:bg-zinc-800 p-3">
      <div className="flex items-center gap-3 mb-2">
        <button
          onClick={handlePlay}
          disabled={isLoading}
          className="flex h-10 w-10 items-center justify-center rounded-full bg-purple-600 dark:bg-purple-700 text-white hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
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
            <p className="text-xs text-red-600 dark:text-red-400">{error}</p>
          ) : (
            <div className="flex items-center gap-2">
              <span className="text-xs text-zinc-600 dark:text-zinc-400 font-mono">
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
            className="w-full h-16 rounded bg-zinc-900 dark:bg-zinc-950"
          />
        </div>
      )}

      {/* Progress Bar */}
      {duration > 0 && (
        <div className="relative">
          <div
            className="h-2 bg-zinc-200 dark:bg-zinc-700 rounded-full cursor-pointer"
            onClick={handleSeek}
          >
            <div
              className="h-full bg-purple-600 dark:bg-purple-500 rounded-full transition-all duration-100"
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
        className="flex items-center gap-1 rounded-md border border-zinc-300 dark:border-zinc-600 bg-white dark:bg-zinc-700 px-2 py-1 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-600 transition-colors"
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
          <div className="absolute right-0 top-full mt-1 z-20 rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 shadow-lg py-1 min-w-[120px]">
            {rates.map((rate) => (
              <button
                key={rate}
                onClick={() => {
                  onChange(rate);
                  setIsOpen(false);
                }}
                className={`w-full px-3 py-1.5 text-left text-xs hover:bg-zinc-100 dark:hover:bg-zinc-700 transition-colors ${
                  playbackRate === rate
                    ? "bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300 font-medium"
                    : "text-zinc-700 dark:text-zinc-300"
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
        const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        const analyser = audioContext.createAnalyser();
        const source = audioContext.createMediaElementSource(audio);
        
        analyser.fftSize = 256;
        source.connect(analyser);
        analyser.connect(audioContext.destination);
        
        audioContextRef.current = audioContext;
        analyserRef.current = analyser;
      } catch (err) {
        console.error('Error setting up audio context:', err);
      }
    };

    if (audio.src) {
      setupAudioContext();
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
        const url = await loadAudioFromMediaId(parsedData.responseMediaId);
        if (audioRef.current && url) {
          audioRef.current.src = url;
          audioRef.current.playbackRate = playbackRate;
          await audioRef.current.play();
          setIsPlaying(true);
        }
      } catch (err) {
        setError("Erro ao reproduzir áudio");
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
          className="flex h-8 w-8 items-center justify-center rounded-full bg-purple-600 dark:bg-purple-700 text-white hover:bg-purple-700 dark:hover:bg-purple-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
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
            <span className="text-xs text-red-600 dark:text-red-400">{error}</span>
          ) : parsedData?.responseMediaId ? (
            <span className="text-xs text-zinc-600 dark:text-zinc-400 font-mono">
              {formatTime(currentTime)} / {formatTime(duration)}
            </span>
          ) : (
            <span className="text-xs text-zinc-500 dark:text-zinc-400">
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
            className="w-full h-12 rounded bg-zinc-900 dark:bg-zinc-950"
          />
        </div>
      )}

      {/* Progress Bar (only for pre-generated audio) */}
      {parsedData?.responseMediaId && duration > 0 && (
        <div className="relative">
          <div
            className="h-1.5 bg-zinc-200 dark:bg-zinc-700 rounded-full cursor-pointer"
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
              className="h-full bg-purple-600 dark:bg-purple-500 rounded-full transition-all duration-100"
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
