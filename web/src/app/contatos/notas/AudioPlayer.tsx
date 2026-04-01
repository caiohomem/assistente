"use client";

import { useState, useRef, useEffect } from "react";
import { getApiBaseUrl } from "@/lib/bff";

function formatTime(seconds: number): string {
  if (isNaN(seconds)) return '0:00';
  const mins = Math.floor(seconds / 60);
  const secs = Math.floor(seconds % 60);
  return `${mins}:${secs.toString().padStart(2, '0')}`;
}

type WindowWithWebkitAudioContext = Window &
  typeof globalThis & {
    webkitAudioContext?: typeof AudioContext;
  };

const getAudioContextConstructor = (): typeof AudioContext | undefined => {
  if (typeof window === "undefined") {
    return undefined;
  }
  const win = window as WindowWithWebkitAudioContext;
  return win.AudioContext ?? win.webkitAudioContext;
};

const describeError = (error: unknown): string => {
  if (error instanceof Error) {
    return error.message || error.name;
  }
  return String(error);
};

interface AudioPlayerProps {
  noteId: string;
}

export function AudioPlayer({ noteId }: AudioPlayerProps) {
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
    } catch (error) {
      setError("Erro ao carregar áudio");
      throw error;
    } finally {
      setIsLoading(false);
    }
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
        canvas.height = 64;
      }
    };
    
    resizeCanvas();
    window.addEventListener('resize', resizeCanvas);
    
    const setupAudioContext = async () => {
      try {
        let audioContext = audioContextRef.current;
        
        if (!audioContext) {
          const AudioContextConstructor = getAudioContextConstructor();
          if (!AudioContextConstructor) {
            console.warn("Web Audio API não está disponível neste navegador.");
            return;
          }
          audioContext = new AudioContextConstructor();
        }
        
        if (audioContext.state === 'suspended') {
          await audioContext.resume();
        }
        
        if (!analyserRef.current) {
          const analyser = audioContext.createAnalyser();
          analyser.fftSize = 256;
          
          try {
            const source = audioContext.createMediaElementSource(audio);
            source.connect(analyser);
            analyser.connect(audioContext.destination);
            analyserRef.current = analyser;
          } catch (error: unknown) {
            console.warn('Audio visualization not available:', describeError(error));
            analyserRef.current = null;
          }
        }
        
        audioContextRef.current = audioContext;
      } catch (error: unknown) {
        console.warn('Error setting up audio context for visualization:', describeError(error));
        audioContextRef.current = null;
        analyserRef.current = null;
      }
    };

    if (audio.src) {
      setupAudioContext().catch((error: unknown) => {
        console.warn('Failed to setup audio context:', describeError(error));
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
        ctx.fillStyle = 'rgba(0, 0, 0, 0)';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
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
        gradient.addColorStop(0, 'rgba(147, 51, 234, 0.9)');
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
        if (audioContextRef.current && audioContextRef.current.state === 'suspended') {
          audioContextRef.current.resume().catch((error: unknown) => {
            console.warn('Failed to resume AudioContext:', describeError(error));
          });
        }
        
        audioRef.current.src = url;
        audioRef.current.playbackRate = playbackRate;
        
        const playPromise = audioRef.current.play();
        if (playPromise !== undefined) {
          await playPromise;
        }
        
        setIsPlaying(true);
      }
    } catch (error: unknown) {
      console.error('Error playing audio:', error);
      let errorMessage = "Erro ao reproduzir áudio";
      if (error instanceof Error) {
        if (error.message) {
          errorMessage = error.message;
        }
        if (error.name === 'NotSupportedError' || error.name === 'NotAllowedError') {
          errorMessage = "Reprodução de áudio não suportada. Verifique as permissões do navegador.";
        }
      } else if (error != null) {
        errorMessage = String(error);
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
      
      {audioBlobUrl && (
        <div className="mb-2">
          <canvas
            ref={canvasRef}
            className="w-full h-16 rounded-lg bg-secondary/50"
          />
        </div>
      )}

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



