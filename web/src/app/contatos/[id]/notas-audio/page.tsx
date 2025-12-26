"use client";

import { useState, useRef } from "react";
import { useParams, useRouter } from "next/navigation";
import { processAudioNote } from "@/lib/api/captureApi";
import type { CaptureJob, JobStatus, JobType } from "@/lib/types/capture";
import { JobStatus as JobStatusEnum, JobType as JobTypeEnum } from "@/lib/types/capture";
import { TopBar } from "@/components/TopBar";

export default function NotasAudioPage() {
  const params = useParams();
  const router = useRouter();
  const contactId = params.id as string;

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
      };

      setJob(jobData);
      setIsUploading(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Erro ao enviar áudio.");
      setIsUploading(false);
    }
  };

  // Polling removido - o processamento é síncrono e retorna resultado completo

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
        return "bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300";
      case JobStatusEnum.Processing:
        return "bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300";
      case JobStatusEnum.Succeeded:
        return "bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300";
      case JobStatusEnum.Failed:
        return "bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300";
      case JobStatusEnum.Cancelled:
        return "bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-300";
      default:
        return "bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-300";
    }
  };

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
      <TopBar title="Notas de Áudio" showBackButton backHref={`/contatos/${contactId}`} />
      <div className="mx-auto max-w-4xl px-4 sm:px-6 py-8 sm:py-12">
        <div className="mb-6">
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            Grave um áudio ou envie um arquivo de áudio para criar uma nota transcrita e resumida automaticamente.
          </p>
        </div>

        {error && (
          <div className="mb-6 rounded-lg border border-red-300 dark:border-red-700 bg-red-50 dark:bg-red-900/30 p-4 text-sm font-medium text-red-800 dark:text-red-300 shadow-sm">
            {error}
          </div>
        )}

        {!job && (
          <div className="rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 shadow-sm p-6 sm:p-8">
            <h2 className="mb-6 text-xl font-bold text-zinc-900 dark:text-zinc-100">Enviar Áudio</h2>

            {/* Gravação */}
            <div className="mb-6">
              <label className="mb-3 block text-sm font-semibold text-zinc-700 dark:text-zinc-300">
                Gravar Áudio
              </label>
              <div className="flex flex-wrap items-center gap-4">
                {!isRecording ? (
                  <button
                    onClick={startRecording}
                    disabled={isUploading}
                    className="flex items-center gap-2 rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-blue-700 hover:shadow-md active:scale-95 disabled:bg-zinc-400 dark:disabled:bg-zinc-600 disabled:cursor-not-allowed disabled:hover:shadow-sm disabled:active:scale-100"
                  >
                    <span className="text-base">🎤</span>
                    Iniciar Gravação
                  </button>
                ) : (
                  <button
                    onClick={stopRecording}
                    disabled={isUploading}
                    className="flex items-center gap-2 rounded-lg bg-red-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-red-700 hover:shadow-md active:scale-95 disabled:bg-zinc-400 dark:disabled:bg-zinc-600 disabled:cursor-not-allowed disabled:hover:shadow-sm disabled:active:scale-100"
                  >
                    <span className="text-base">⏹️</span>
                    Parar Gravação
                  </button>
                )}
                {isRecording && (
                  <span className="flex items-center gap-2 text-sm font-medium text-red-600 dark:text-red-400">
                    <span className="h-2.5 w-2.5 animate-pulse rounded-full bg-red-600"></span>
                    Gravando...
                  </span>
                )}
              </div>
            </div>

            {/* Separador */}
            <div className="mb-6 flex items-center gap-3">
              <div className="flex-1 border-t border-zinc-300 dark:border-zinc-600"></div>
              <span className="text-xs font-medium text-zinc-500 dark:text-zinc-400">OU</span>
              <div className="flex-1 border-t border-zinc-300 dark:border-zinc-600"></div>
            </div>

            {/* Upload de arquivo */}
            <div className="mb-6">
              <label className="mb-3 block text-sm font-semibold text-zinc-700 dark:text-zinc-300">
                Enviar Arquivo de Áudio
              </label>
              <div className="space-y-3">
                <input
                  type="file"
                  accept="audio/mpeg,audio/mp3,audio/wav,audio/webm,audio/ogg,audio/m4a,audio/aac"
                  onChange={handleFileChange}
                  disabled={isUploading}
                  className="block w-full text-sm text-zinc-600 dark:text-zinc-400
                    file:mr-4 file:rounded-lg file:border-0 
                    file:bg-blue-50 dark:file:bg-blue-900/30
                    file:px-4 file:py-2.5 
                    file:text-sm file:font-semibold 
                    file:text-blue-700 dark:file:text-blue-300
                    hover:file:bg-blue-100 dark:hover:file:bg-blue-900/50
                    file:transition-colors file:cursor-pointer
                    disabled:opacity-50 disabled:cursor-not-allowed
                    cursor-pointer"
                />
                {file && (
                  <div className="flex items-center justify-between rounded-md bg-zinc-50 dark:bg-zinc-900/50 border border-zinc-200 dark:border-zinc-700 p-3 text-sm text-zinc-700 dark:text-zinc-300">
                    <div>
                      <span className="font-medium">Arquivo selecionado:</span>{" "}
                      <span className="font-semibold text-zinc-900 dark:text-zinc-100">{file.name}</span>{" "}
                      <span className="text-zinc-500 dark:text-zinc-400">
                        ({(file.size / 1024 / 1024).toFixed(2)} MB)
                      </span>
                    </div>
                    <button
                      onClick={() => {
                        setFile(null);
                        // Limpar o input file
                        const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement;
                        if (fileInput) {
                          fileInput.value = '';
                        }
                      }}
                      className="ml-3 text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 transition-colors"
                      title="Remover arquivo"
                    >
                      ✕
                    </button>
                  </div>
                )}
                <p className="text-xs text-zinc-500 dark:text-zinc-400">
                  Formatos suportados: MP3, WAV, WebM, OGG, M4A, AAC (máx. 50MB)
                </p>
              </div>
            </div>

            {/* Botão de upload */}
            <button
              onClick={handleUpload}
              disabled={!file || isUploading}
              className="w-full rounded-lg bg-green-600 px-6 py-3.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-green-700 hover:shadow-md active:scale-[0.98] disabled:bg-zinc-400 dark:disabled:bg-zinc-600 disabled:cursor-not-allowed disabled:hover:shadow-sm disabled:active:scale-100"
            >
              {isUploading ? "Enviando..." : "Enviar e Processar"}
            </button>
          </div>
        )}

        {/* Status do Job */}
        {job && (
          <div className="mt-6 rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 shadow-sm p-6 sm:p-8">
            <div className="mb-6 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
              <h2 className="text-xl font-bold text-zinc-900 dark:text-zinc-100">Status do Processamento</h2>
              <span
                className={`inline-flex items-center rounded-full px-3 py-1.5 text-xs font-semibold shadow-sm ${getStatusColor(job.status)}`}
              >
                {getStatusText(job.status)}
              </span>
            </div>


            {job.errorMessage && (
              <div className="mb-4 rounded-lg border border-red-300 dark:border-red-700 bg-red-50 dark:bg-red-900/30 p-4 text-sm font-medium text-red-800 dark:text-red-300 shadow-sm">
                <strong>Erro:</strong> {job.errorMessage}
                {job.errorCode && ` (${job.errorCode})`}
              </div>
            )}

            {/* Transcrição */}
            {job.audioTranscript && (
              <div className="mb-6">
                <h3 className="mb-3 text-sm font-semibold text-zinc-700 dark:text-zinc-300">Transcrição</h3>
                <div className="rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-900/50 p-4 text-sm text-zinc-800 dark:text-zinc-200 leading-relaxed">
                  {job.audioTranscript.text}
                </div>
                {job.audioTranscript.segments && job.audioTranscript.segments.length > 0 && (
                  <details className="mt-3">
                    <summary className="cursor-pointer rounded-md bg-zinc-100 dark:bg-zinc-800 px-3 py-2 text-xs font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-200 dark:hover:bg-zinc-700 transition-colors">
                      Ver segmentos ({job.audioTranscript.segments.length})
                    </summary>
                    <div className="mt-3 space-y-2">
                      {job.audioTranscript.segments.map((segment, idx) => (
                        <div key={idx} className="rounded-lg border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 p-3 text-xs">
                          <div className="text-zinc-600 dark:text-zinc-400 font-medium mb-1">
                            {segment.startTime} - {segment.endTime} (confiança:{" "}
                            {(segment.confidence * 100).toFixed(1)}%)
                          </div>
                          <div className="mt-1 text-zinc-800 dark:text-zinc-200">{segment.text}</div>
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
                <h3 className="mb-3 text-sm font-semibold text-zinc-700 dark:text-zinc-300">Resumo</h3>
                <div className="rounded-lg border border-blue-200 dark:border-blue-800 bg-blue-50 dark:bg-blue-900/20 p-4 text-sm text-zinc-800 dark:text-zinc-200 leading-relaxed">
                  {job.audioSummary}
                </div>
              </div>
            )}

            {/* Tarefas extraídas */}
            {job.extractedTasks && job.extractedTasks.length > 0 && (
              <div className="mb-6">
                <h3 className="mb-3 text-sm font-semibold text-zinc-700 dark:text-zinc-300">Tarefas Extraídas</h3>
                <div className="space-y-3">
                  {job.extractedTasks.map((task, idx) => (
                    <div
                      key={idx}
                      className="rounded-lg border border-zinc-200 dark:border-zinc-700 bg-zinc-50 dark:bg-zinc-900/50 p-4 text-sm shadow-sm"
                    >
                      <div className="font-semibold text-zinc-900 dark:text-zinc-100">{task.description}</div>
                      {(task.dueDate || task.priority) && (
                        <div className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
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
            <div className="mt-6 border-t border-zinc-200 dark:border-zinc-700 pt-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                <div className="text-zinc-600 dark:text-zinc-400">
                  <strong className="font-semibold text-zinc-700 dark:text-zinc-300">Job ID:</strong>{" "}
                  <span className="font-mono">{job.jobId}</span>
                </div>
                <div className="text-zinc-600 dark:text-zinc-400">
                  <strong className="font-semibold text-zinc-700 dark:text-zinc-300">Solicitado em:</strong>{" "}
                  {new Date(job.requestedAt).toLocaleString("pt-BR")}
                </div>
                {job.completedAt && (
                  <div className="text-zinc-600 dark:text-zinc-400">
                    <strong className="font-semibold text-zinc-700 dark:text-zinc-300">Concluído em:</strong>{" "}
                    {new Date(job.completedAt).toLocaleString("pt-BR")}
                  </div>
                )}
              </div>
            </div>

            {/* Botões de ação */}
            <div className="mt-6 flex flex-col sm:flex-row gap-3">
              <button
                onClick={() => {
                  setJob(null);
                  setFile(null);
                  setError(null);
                }}
                className="flex-1 sm:flex-initial rounded-lg bg-zinc-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-zinc-700 hover:shadow-md active:scale-95"
              >
                Novo Áudio
              </button>
              <button
                onClick={() => router.push(`/contatos/${contactId}`)}
                className="flex-1 sm:flex-initial rounded-lg bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all hover:bg-blue-700 hover:shadow-md active:scale-95"
              >
                Ver Contato
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

