"use client";

import { useState, useRef } from "react";
import { processAudioNote } from "@/lib/api/captureApi";
import type { CaptureJob, JobStatus, JobType } from "@/lib/types/capture";
import { JobStatus as JobStatusEnum, JobType as JobTypeEnum } from "@/lib/types/capture";
import { Button } from "@/components/ui/button";
import { getApiBaseUrl } from "@/lib/bff";
import { Mic, StopCircle, Upload, X } from "lucide-react";

function formatTime(seconds: number): string {
  if (isNaN(seconds)) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

interface NovaNotaAudioClientProps {
  contactId: string;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function NovaNotaAudioClient({ contactId, onSuccess, onCancel }: NovaNotaAudioClientProps) {
  const [file, setFile] = useState<File | null>(null);
  const [isRecording, setIsRecording] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [job, setJob] = useState<CaptureJob | null>(null);

  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);

  const startRecording = async () => {
    try {
      // Limpar arquivo selecionado se houver, pois vamos gravar um novo
      if (file) {
        setFile(null);
      }

      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const mediaRecorder = new MediaRecorder(stream);
      mediaRecorderRef.current = mediaRecorder;
      audioChunksRef.current = [];

      mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          audioChunksRef.current.push(event.data);
        }
      };

      mediaRecorder.onstop = () => {
        const audioBlob = new Blob(audioChunksRef.current, { type: "audio/webm" });
        const audioFile = new File([audioBlob], "recording.webm", { type: "audio/webm" });
        setFile(audioFile);
        stream.getTracks().forEach((track) => track.stop());
      };

      mediaRecorder.start();
      setIsRecording(true);
      setError(null);
    } catch (err) {
      setError("Erro ao acessar o microfone. Verifique as permissões.");
      console.error("Erro ao iniciar gravação:", err);
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      // Validar tipo de arquivo
      const allowedTypes = ["audio/mpeg", "audio/mp3", "audio/wav", "audio/webm", "audio/ogg", "audio/m4a", "audio/aac"];
      if (!allowedTypes.includes(selectedFile.type)) {
        setError("Tipo de arquivo não suportado. Use MP3, WAV, WebM, OGG, M4A ou AAC.");
        return;
      }

      // Validar tamanho (máximo 50MB)
      const maxSize = 50 * 1024 * 1024; // 50MB
      if (selectedFile.size > maxSize) {
        setError("Arquivo muito grande. Tamanho máximo: 50MB.");
        return;
      }

      // Se estiver gravando, parar a gravação antes de selecionar o arquivo
      if (isRecording && mediaRecorderRef.current) {
        mediaRecorderRef.current.stop();
        setIsRecording(false);
      }

      setFile(selectedFile);
      setError(null);
    }
  };

  const handleUpload = async () => {
    if (!file) {
      setError("Selecione ou grave um arquivo de áudio.");
      return;
    }

    setIsUploading(true);
    setError(null);
    setJob(null);

    try {
      const result = await processAudioNote({
        file,
        contactId,
      });

      // Converter resposta para formato CaptureJob
      const jobData: CaptureJob = {
        jobId: result.jobId,
        ownerUserId: "", // Não necessário para exibição
        type: JobTypeEnum.AudioNoteTranscription,
        contactId: contactId,
        mediaId: result.mediaId,
        status: result.status === "Succeeded" ? JobStatusEnum.Succeeded : 
                result.status === "Failed" ? JobStatusEnum.Failed :
                result.status === "Processing" ? JobStatusEnum.Processing :
                JobStatusEnum.Requested,
        requestedAt: result.requestedAt,
        completedAt: result.completedAt ?? null,
        errorCode: result.errorCode ?? null,
        errorMessage: result.errorMessage ?? null,
        audioTranscript: result.audioTranscript ?? null,
        audioSummary: result.audioSummary ?? null,
        extractedTasks: result.extractedTasks ?? null,
        responseMediaId: result.responseMediaId ?? null,
      };

      setJob(jobData);
      setIsUploading(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao enviar áudio.");
      setIsUploading(false);
    }
  };

  const getStatusText = (status: JobStatus): string => {
    switch (status) {
      case JobStatusEnum.Requested:
        return "Solicitado";
      case JobStatusEnum.Processing:
        return "Processando...";
      case JobStatusEnum.Succeeded:
        return "Concluído";
      case JobStatusEnum.Failed:
        return "Falhou";
      case JobStatusEnum.Cancelled:
        return "Cancelado";
      default:
        return "Desconhecido";
    }
  };

  const getStatusColor = (status: JobStatus): string => {
    switch (status) {
      case JobStatusEnum.Requested:
        return "bg-primary/20 text-primary border-primary/30";
      case JobStatusEnum.Processing:
        return "bg-warning/20 text-warning border-warning/30";
      case JobStatusEnum.Succeeded:
        return "bg-success/20 text-success border-success/30";
      case JobStatusEnum.Failed:
        return "bg-destructive/20 text-destructive border-destructive/30";
      case JobStatusEnum.Cancelled:
        return "bg-muted text-muted-foreground border-border";
      default:
        return "bg-muted text-muted-foreground border-border";
    }
  };

  return (
    <div className="space-y-6">
      {error && (
        <div className="glass-card border-destructive/50 bg-destructive/10 p-4">
          <p className="text-sm text-destructive">{error}</p>
        </div>
      )}

      {!job && (
        <>
          <div>
            <p className="text-sm text-muted-foreground mb-4">
              Grave um áudio ou envie um arquivo de áudio para criar uma nota transcrita e resumida automaticamente.
            </p>
          </div>

          {/* Gravação */}
          <div>
            <label className="mb-3 block text-sm font-medium text-foreground">
              Gravar Áudio
            </label>
            <div className="flex flex-wrap items-center gap-4">
              {!isRecording ? (
                <Button
                  onClick={startRecording}
                  disabled={isUploading}
                  variant="glow"
                  className="flex items-center gap-2"
                >
                  <Mic className="w-4 h-4" />
                  Iniciar Gravação
                </Button>
              ) : (
                <Button
                  onClick={stopRecording}
                  disabled={isUploading}
                  variant="destructive"
                  className="flex items-center gap-2"
                >
                  <StopCircle className="w-4 h-4" />
                  Parar Gravação
                </Button>
              )}
              {isRecording && (
                <span className="flex items-center gap-2 text-sm font-medium text-destructive">
                  <span className="h-2.5 w-2.5 animate-pulse rounded-full bg-destructive"></span>
                  Gravando...
                </span>
              )}
            </div>
          </div>

          {/* Separador */}
          <div className="flex items-center gap-3">
            <div className="flex-1 border-t border-border"></div>
            <span className="text-xs font-medium text-muted-foreground">OU</span>
            <div className="flex-1 border-t border-border"></div>
          </div>

          {/* Upload de arquivo */}
          <div>
            <label className="mb-3 block text-sm font-medium text-foreground">
              Enviar Arquivo de Áudio
            </label>
            <div className="space-y-3">
              <input
                type="file"
                accept="audio/mpeg,audio/mp3,audio/wav,audio/webm,audio/ogg,audio/m4a,audio/aac"
                onChange={handleFileChange}
                disabled={isUploading}
                className="block w-full text-sm text-foreground
                  file:mr-4 file:rounded-xl file:border-0 
                  file:bg-primary/10
                  file:px-4 file:py-2.5 
                  file:text-sm file:font-semibold 
                  file:text-primary
                  hover:file:bg-primary/20
                  file:transition-colors file:cursor-pointer
                  disabled:opacity-50 disabled:cursor-not-allowed
                  cursor-pointer"
              />
              {file && (
                <div className="flex items-center justify-between glass-card border-border/50 p-3 text-sm">
                  <div className="flex-1 min-w-0">
                    <span className="font-medium text-foreground">Arquivo selecionado: </span>
                    <span className="font-semibold text-foreground">{file.name}</span>{" "}
                    <span className="text-muted-foreground">
                      ({(file.size / 1024 / 1024).toFixed(2)} MB)
                    </span>
                  </div>
                  <Button
                    onClick={() => {
                      setFile(null);
                      // Limpar o input file
                      const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
                      if (fileInput) {
                        fileInput.value = '';
                      }
                    }}
                    variant="ghost"
                    size="icon"
                    className="ml-3 h-8 w-8 flex-shrink-0"
                    title="Remover arquivo"
                  >
                    <X className="w-4 h-4" />
                  </Button>
                </div>
              )}
              <p className="text-xs text-muted-foreground">
                Formatos suportados: MP3, WAV, WebM, OGG, M4A, AAC (máx. 50MB)
              </p>
            </div>
          </div>

          {/* Botão de upload */}
          <div className="flex gap-4 justify-end">
            {onCancel && (
              <Button
                onClick={onCancel}
                variant="ghost"
                disabled={isUploading}
              >
                Cancelar
              </Button>
            )}
            <Button
              onClick={handleUpload}
              disabled={!file || isUploading}
              variant="glow"
              className="flex items-center justify-center gap-2"
            >
              <Upload className="w-4 h-4" />
              {isUploading ? "Enviando..." : "Enviar e Processar"}
            </Button>
          </div>
        </>
      )}

      {/* Status do Job */}
      {job && (
        <div className="glass-card p-6">
          <div className="mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <h2 className="text-xl font-semibold">Status do Processamento</h2>
            <span
              className={`inline-flex items-center rounded-full border px-3 py-1.5 text-xs font-semibold ${getStatusColor(job.status)}`}
            >
              {getStatusText(job.status)}
            </span>
          </div>

          {job.errorMessage && (
            <div className="mb-4 glass-card border-destructive/50 bg-destructive/10 p-4">
              <p className="text-sm text-destructive">
                <strong>Erro:</strong> {job.errorMessage}
                {job.errorCode && ` (${job.errorCode})`}
              </p>
            </div>
          )}

          {/* Transcrição */}
          {job.audioTranscript && (
            <div className="mb-6">
              <h3 className="mb-3 text-sm font-semibold text-foreground">Transcrição</h3>
              <div className="glass-card border-border/50 p-4 text-sm leading-relaxed">
                {job.audioTranscript.text}
              </div>
              {job.audioTranscript.segments && job.audioTranscript.segments.length > 0 && (
                <details className="mt-3">
                  <summary className="cursor-pointer glass-card border-border/50 px-3 py-2 text-xs font-medium text-muted-foreground hover:text-foreground transition-colors">
                    Ver segmentos ({job.audioTranscript.segments.length})
                  </summary>
                  <div className="mt-3 space-y-2">
                    {job.audioTranscript.segments.map((segment, idx) => (
                      <div key={idx} className="glass-card border-border/50 p-3 text-xs">
                        <div className="text-muted-foreground font-medium mb-1">
                          {segment.startTime} - {segment.endTime} (confiança:{" "}
                          {(segment.confidence * 100).toFixed(1)}%)
                        </div>
                        <div className="mt-1 text-foreground">{segment.text}</div>
                      </div>
                    ))}
                  </div>
                </details>
              )}
            </div>
          )}

          {/* Resumo */}
          {job.audioSummary && (
            <div className="mb-6">
              <h3 className="mb-3 text-sm font-semibold text-foreground">Resumo</h3>
              <div className="glass-card border-primary/20 bg-primary/5 p-4 text-sm leading-relaxed">
                {job.audioSummary}
              </div>
            </div>
          )}

          {/* Tarefas extraídas */}
          {job.extractedTasks && job.extractedTasks.length > 0 && (
            <div className="mb-6">
              <h3 className="mb-3 text-sm font-semibold text-foreground">Tarefas Extraídas</h3>
              <div className="space-y-3">
                {job.extractedTasks.map((task, idx) => (
                  <div
                    key={idx}
                    className="glass-card border-border/50 p-4 text-sm"
                  >
                    <div className="font-semibold text-foreground">{task.description}</div>
                    {(task.dueDate || task.priority) && (
                      <div className="mt-2 text-xs text-muted-foreground">
                        {task.dueDate && `Prazo: ${new Date(task.dueDate).toLocaleDateString("pt-BR")}`}
                        {task.dueDate && task.priority && " • "}
                        {task.priority && `Prioridade: ${task.priority}`}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Informações do Job */}
          <div className="mt-6 border-t border-border pt-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
              <div className="text-muted-foreground">
                <strong className="font-semibold text-foreground">Job ID:</strong>{" "}
                <span className="font-mono">{job.jobId}</span>
              </div>
              <div className="text-muted-foreground">
                <strong className="font-semibold text-foreground">Solicitado em:</strong>{" "}
                {new Date(job.requestedAt).toLocaleString("pt-BR")}
              </div>
              {job.completedAt && (
                <div className="text-muted-foreground">
                  <strong className="font-semibold text-foreground">Concluído em:</strong>{" "}
                  {new Date(job.completedAt).toLocaleString("pt-BR")}
                </div>
              )}
            </div>
          </div>

          {/* Botões de ação */}
          {onSuccess && job.status === JobStatusEnum.Succeeded && (
            <div className="mt-6 flex gap-3">
              <Button
                onClick={() => {
                  setJob(null);
                  setFile(null);
                  setError(null);
                }}
                variant="ghost"
              >
                Novo Áudio
              </Button>
              <Button
                onClick={onSuccess}
                variant="glow"
                className="flex-1"
              >
                Concluir
              </Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

