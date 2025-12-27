"use client";

import { useState, useRef, useEffect } from "react";
import { useParams, useRouter } from "next/navigation";
import { processAudioNote } from "@/lib/api/captureApi";
import type { CaptureJob, JobStatus, JobType } from "@/lib/types/capture";
import { JobStatus as JobStatusEnum, JobType as JobTypeEnum } from "@/lib/types/capture";
import { TopBar } from "@/components/TopBar";
import { getApiBaseUrl } from "@/lib/bff";

function formatTime(seconds: number): string {
  if (isNaN(seconds)) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

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
        responseMediaId: result.responseMediaId ?? null,
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
                <div className="mb-3 flex items-center justify-between">
                  <h3 className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">Resumo</h3>
                  {job.responseMediaId && (
                    <TTSPlayer mediaId={job.responseMediaId} />
                  )}
                </div>
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

function TTSPlayer({ mediaId }: { mediaId: string }) {
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

  function handleEnded() {
    setIsPlaying(false);
  }

  function handlePause() {
    setIsPlaying(false);
  }

  // Setup Web Audio API for visualization
  useEffect(() => {
    if (!audioRef.current || !canvasRef.current || !audioBlobUrl) return;

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
        gradient.addColorStop(0, 'rgba(37, 99, 235, 0.9)'); // blue-600
        gradient.addColorStop(1, 'rgba(37, 99, 235, 0.2)');

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
  }, [isPlaying]);

  function handleSeek(e: React.MouseEvent<HTMLDivElement>) {
    if (!audioRef.current || !duration) return;

    const rect = e.currentTarget.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const percentage = x / rect.width;
    const newTime = percentage * duration;

    audioRef.current.currentTime = newTime;
    setCurrentTime(newTime);
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
    <div className="w-full">
      <div className="flex items-center gap-2 mb-2">
        <button
          onClick={handlePlay}
          disabled={isLoading}
          className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-600 dark:bg-blue-700 text-white hover:bg-blue-700 dark:hover:bg-blue-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex-shrink-0"
          aria-label={isPlaying ? "Pausar áudio" : "Reproduzir resumo em áudio"}
          title={isPlaying ? "Pausar áudio" : "Reproduzir resumo em áudio"}
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
          ) : (
            <span className="text-xs text-zinc-600 dark:text-zinc-400 font-mono">
              {formatTime(currentTime)} / {formatTime(duration)}
            </span>
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
            className="w-full h-12 rounded bg-zinc-900 dark:bg-zinc-950"
          />
        </div>
      )}

      {/* Progress Bar */}
      {duration > 0 && (
        <div className="relative">
          <div
            className="h-1.5 bg-zinc-200 dark:bg-zinc-700 rounded-full cursor-pointer"
            onClick={handleSeek}
          >
            <div
              className="h-full bg-blue-600 dark:bg-blue-500 rounded-full transition-all duration-100"
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
                    ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-medium"
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

