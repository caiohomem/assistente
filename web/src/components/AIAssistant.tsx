"use client"

import { Send, Sparkles, Mic, Paperclip, Loader2, StopCircle, Image as ImageIcon, Upload } from "lucide-react";
import { Button } from "./ui/button";
import { useState, useRef } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { uploadCard, processAudioNote, getCaptureJobById, transcribeAudio } from "@/lib/api/captureApi";
import type { UploadCardResponse, ProcessAudioNoteResponse } from "@/lib/api/captureApi";

interface Message {
  id: string;
  role: "user" | "assistant";
  content: string;
  timestamp: string;
}

export function AIAssistant() {
  const [input, setInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [messages, setMessages] = useState<Message[]>([
    {
      id: "1",
      role: "assistant",
      content: "Olá! Sou seu assistente executivo com IA. Posso ajudar a gerenciar seus contatos, analisar cartões de visita, gravar notas de áudio e encontrar conexões entre seus relacionamentos. Como posso ajudá-lo hoje?",
      timestamp: "Agora"
    }
  ]);

  // Estados para upload de cartão e gravação de áudio
  const [isRecording, setIsRecording] = useState(false);
  const [isUploadingCard, setIsUploadingCard] = useState(false);
  const [isUploadingAudio, setIsUploadingAudio] = useState(false);
  const [recordingTime, setRecordingTime] = useState(0);
  
  const fileInputRef = useRef<HTMLInputElement>(null);
  const audioInputRef = useRef<HTMLInputElement>(null);
  const mediaRecorderRef = useRef<MediaRecorder | null>(null);
  const audioChunksRef = useRef<Blob[]>([]);
  const recordingIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const streamRef = useRef<MediaStream | null>(null);

  const formatTimestamp = () => {
    return new Date().toLocaleTimeString("pt-BR", { 
      hour: "2-digit", 
      minute: "2-digit" 
    });
  };

  const formatTime = (seconds: number): string => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  // ========== Funções de Upload de Cartão ==========
  
  const handleCardUploadClick = () => {
    fileInputRef.current?.click();
  };

  const handleCardFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validar tipo de arquivo (imagem)
    const allowedTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      addErrorMessage("Tipo de arquivo não suportado. Use JPEG, PNG ou WebP.");
      return;
    }

    // Validar tamanho (máximo 10MB)
    const maxSize = 10 * 1024 * 1024; // 10MB
    if (file.size > maxSize) {
      addErrorMessage("Arquivo muito grande. Tamanho máximo: 10MB.");
      return;
    }

    setIsUploadingCard(true);
    
    // Adicionar mensagem de upload
    const uploadMessage: Message = {
      id: Date.now().toString(),
      role: "user",
      content: `📷 Enviando cartão de visita: ${file.name}...`,
      timestamp: formatTimestamp()
    };
    setMessages(prev => [...prev, uploadMessage]);

    try {
      const result: UploadCardResponse = await uploadCard({ file });
      
      // Adicionar mensagem de sucesso
      const successMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: `✅ Cartão processado com sucesso! Contato criado: ${result.contactId}. Processando OCR...`,
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, successMessage]);

      // Aguardar processamento e obter resultado
      setTimeout(async () => {
        try {
          const job = await getCaptureJobById(result.jobId);
          if (job.cardScanResult) {
            const ocrMessage: Message = {
              id: (Date.now() + 2).toString(),
              role: "assistant",
              content: `📋 Dados extraídos do cartão:\n\n**Nome:** ${job.cardScanResult.name || "N/A"}\n**Email:** ${job.cardScanResult.email || "N/A"}\n**Telefone:** ${job.cardScanResult.phone || "N/A"}\n**Empresa:** ${job.cardScanResult.company || "N/A"}\n**Cargo:** ${job.cardScanResult.jobTitle || "N/A"}`,
              timestamp: formatTimestamp()
            };
            setMessages(prev => [...prev, ocrMessage]);
          }
        } catch (err) {
          console.error("Erro ao obter resultado do OCR:", err);
        }
      }, 2000);

    } catch (error) {
      addErrorMessage(`Erro ao processar cartão: ${error instanceof Error ? error.message : "Erro desconhecido"}`);
    } finally {
      setIsUploadingCard(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = "";
      }
    }
  };

  // ========== Funções de Gravação de Áudio ==========

  const startRecording = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      streamRef.current = stream;
      
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
        const audioFile = new File([audioBlob], `recording-${Date.now()}.webm`, { type: "audio/webm" });
        handleAudioUpload(audioFile);
        stream.getTracks().forEach((track) => track.stop());
        streamRef.current = null;
      };

      mediaRecorder.start();
      setIsRecording(true);
      setRecordingTime(0);
      
      // Iniciar contador de tempo
      recordingIntervalRef.current = setInterval(() => {
        setRecordingTime(prev => prev + 1);
      }, 1000);

      // Adicionar mensagem de gravação
      const recordingMessage: Message = {
        id: Date.now().toString(),
        role: "user",
        content: "🎤 Gravando áudio...",
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, recordingMessage]);

    } catch (err) {
      addErrorMessage("Erro ao acessar o microfone. Verifique as permissões.");
      console.error("Erro ao iniciar gravação:", err);
    }
  };

  const stopRecording = () => {
    if (mediaRecorderRef.current && isRecording) {
      mediaRecorderRef.current.stop();
      setIsRecording(false);
      
      if (recordingIntervalRef.current) {
        clearInterval(recordingIntervalRef.current);
        recordingIntervalRef.current = null;
      }
    }
  };

  const handleAudioFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validar tipo de arquivo (áudio)
    const allowedTypes = ["audio/mpeg", "audio/mp3", "audio/wav", "audio/webm", "audio/ogg", "audio/m4a"];
    if (!allowedTypes.includes(file.type)) {
      addErrorMessage("Tipo de arquivo não suportado. Use MP3, WAV, WebM, OGG ou M4A.");
      return;
    }

    // Validar tamanho (máximo 50MB)
    const maxSize = 50 * 1024 * 1024; // 50MB
    if (file.size > maxSize) {
      addErrorMessage("Arquivo muito grande. Tamanho máximo: 50MB.");
      return;
    }

    // Permitir upload direto - o assistente vai ajudar a encontrar/criar o contato
    handleAudioUpload(file);
  };

  const handleAudioUpload = async (file: File) => {
    setIsUploadingAudio(true);
    
    // Adicionar mensagem de processamento
    const processingMessage: Message = {
      id: Date.now().toString(),
      role: "assistant",
      content: "🔄 Transcrevendo áudio...",
      timestamp: formatTimestamp()
    };
    setMessages(prev => [...prev, processingMessage]);

    try {
      // Transcrever áudio para texto
      const transcript = await transcribeAudio(file);
      
      // Remover mensagem de processamento
      setMessages(prev => prev.filter(m => m.id !== processingMessage.id));
      
      if (!transcript.text || transcript.text.trim().length === 0) {
        addErrorMessage("Não foi possível transcrever o áudio. O arquivo pode estar vazio ou corrompido.");
        return;
      }

      // Adicionar mensagem do usuário com o texto transcrito
      const userMessage: Message = {
        id: Date.now().toString(),
        role: "user",
        content: transcript.text,
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, userMessage]);

      // Enviar o texto transcrito ao assistente como uma mensagem normal
      setLoading(true);
      
      const chatMessages = messages
        .filter(m => m.role !== "assistant" || !m.content.includes("Processando") && !m.content.includes("Transcrevendo"))
        .map(m => ({
          role: m.role,
          content: m.content,
        }));
      
      chatMessages.push({
        role: "user",
        content: transcript.text,
      });

      const loadingMessage: Message = {
        id: (Date.now() + 1).toString(),
        role: "assistant",
        content: "Processando...",
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, loadingMessage]);

      const response = await fetch("/api/assistant/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          messages: chatMessages,
          model: "gpt-4o",
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || "Erro ao processar mensagem");
      }

      const data = await response.json();
      
      // Remover mensagem de carregamento
      setMessages(prev => prev.filter(m => m.id !== loadingMessage.id));
      
      // Adicionar resposta do assistente
      const messageContent = data.message || data.Message || "Não foi possível gerar uma resposta.";
      const aiMessage: Message = {
        id: (Date.now() + 2).toString(),
        role: "assistant",
        content: messageContent,
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, aiMessage]);

    } catch (error) {
      // Remover mensagem de processamento
      setMessages(prev => prev.filter(m => m.id !== processingMessage.id));
      
      addErrorMessage(`Erro ao transcrever áudio: ${error instanceof Error ? error.message : "Erro desconhecido"}`);
    } finally {
      setIsUploadingAudio(false);
      if (audioInputRef.current) {
        audioInputRef.current.value = "";
      }
      setLoading(false);
    }
  };


  const handleMicClick = () => {
    if (isRecording) {
      stopRecording();
    } else {
      startRecording();
    }
  };

  const addErrorMessage = (message: string) => {
    const errorMessage: Message = {
      id: Date.now().toString(),
      role: "assistant",
      content: `❌ ${message}`,
      timestamp: formatTimestamp()
    };
    setMessages(prev => [...prev, errorMessage]);
  };

  const handleSend = async () => {
    if (!input.trim() || loading) return;
    
    const userMessage: Message = {
      id: Date.now().toString(),
      role: "user",
      content: input,
      timestamp: formatTimestamp()
    };
    
    const userInput = input;
    setMessages(prev => [...prev, userMessage]);
    setInput("");
    setLoading(true);

    // Adicionar mensagem de carregamento
    const loadingMessage: Message = {
      id: (Date.now() + 1).toString(),
      role: "assistant",
      content: "Processando...",
      timestamp: formatTimestamp()
    };
    setMessages(prev => [...prev, loadingMessage]);
    
    try {
      // Preparar histórico de mensagens para enviar
      const chatMessages = messages
        .filter(m => m.role !== "assistant" || !m.content.includes("Processando"))
        .map(m => ({
          role: m.role,
          content: m.content,
        }));
      
      chatMessages.push({
        role: "user",
        content: userInput,
      });

      const response = await fetch("/api/assistant/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          messages: chatMessages,
          model: "gpt-4o",
        }),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || "Erro ao processar mensagem");
      }

      const data = await response.json();
      
      // Remover mensagem de carregamento
      setMessages(prev => prev.filter(m => m.id !== loadingMessage.id));
      
      // Adicionar resposta do assistente
      // Aceitar tanto camelCase quanto PascalCase
      const messageContent = data.message || data.Message || "Não foi possível gerar uma resposta.";
      const aiMessage: Message = {
        id: (Date.now() + 2).toString(),
        role: "assistant",
        content: messageContent,
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, aiMessage]);

    } catch (error) {
      // Remover mensagem de carregamento
      setMessages(prev => prev.filter(m => m.id !== loadingMessage.id));
      
      // Adicionar mensagem de erro
      const errorMessage: Message = {
        id: (Date.now() + 2).toString(),
        role: "assistant",
        content: `Erro: ${error instanceof Error ? error.message : "Erro desconhecido"}`,
        timestamp: formatTimestamp()
      };
      setMessages(prev => [...prev, errorMessage]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="glass-card h-[600px] flex flex-col animate-slide-up">
      {/* Header */}
      <div className="p-4 border-b border-border flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary to-accent flex items-center justify-center">
          <Sparkles className="w-5 h-5 text-primary-foreground" />
        </div>
        <div>
          <h2 className="font-semibold">Assistente IA</h2>
          <p className="text-xs text-muted-foreground">Powered by GPT-4 Vision</p>
        </div>
      </div>
      
      {/* Messages */}
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {messages.map((message) => (
          <div
            key={message.id}
            className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}
          >
            <div
              className={`max-w-[80%] rounded-2xl px-4 py-3 ${
                message.role === "user"
                  ? "bg-primary text-primary-foreground rounded-br-md"
                  : "bg-secondary text-foreground rounded-bl-md"
              }`}
            >
              {message.role === "assistant" ? (
                <div className="text-sm">
                  <ReactMarkdown
                    remarkPlugins={[remarkGfm]}
                    components={{
                      p: ({ children }) => <p className="mb-2 last:mb-0 leading-relaxed">{children}</p>,
                      ul: ({ children }) => <ul className="list-disc list-inside mb-2 space-y-1 ml-2">{children}</ul>,
                      ol: ({ children }) => <ol className="list-decimal list-inside mb-2 space-y-1 ml-2">{children}</ol>,
                      li: ({ children }) => <li className="leading-relaxed">{children}</li>,
                      strong: ({ children }) => <strong className="font-semibold">{children}</strong>,
                      em: ({ children }) => <em className="italic">{children}</em>,
                      code: ({ children }) => (
                        <code className="bg-background/50 dark:bg-background/30 px-1.5 py-0.5 rounded text-xs font-mono">
                          {children}
                        </code>
                      ),
                      pre: ({ children }) => (
                        <pre className="bg-background/50 dark:bg-background/30 p-2 rounded text-xs font-mono overflow-x-auto mb-2">
                          {children}
                        </pre>
                      ),
                      h1: ({ children }) => <h1 className="text-lg font-bold mb-2 mt-3 first:mt-0">{children}</h1>,
                      h2: ({ children }) => <h2 className="text-base font-semibold mb-2 mt-3 first:mt-0">{children}</h2>,
                      h3: ({ children }) => <h3 className="text-sm font-semibold mb-1 mt-2 first:mt-0">{children}</h3>,
                      blockquote: ({ children }) => (
                        <blockquote className="border-l-4 border-primary/30 pl-3 italic my-2 opacity-80">
                          {children}
                        </blockquote>
                      ),
                      a: ({ children, href }) => (
                        <a href={href} className="text-primary underline hover:text-primary/80" target="_blank" rel="noopener noreferrer">
                          {children}
                        </a>
                      ),
                      hr: () => <hr className="my-3 border-border opacity-50" />,
                    }}
                  >
                    {message.content}
                  </ReactMarkdown>
                </div>
              ) : (
                <p className="text-sm">{message.content}</p>
              )}
              <p className="text-xs opacity-60 mt-1">{message.timestamp}</p>
            </div>
          </div>
        ))}
      </div>
      
      {/* Input */}
      <div className="p-4 border-t border-border">
        <div className="flex items-center gap-2">
          {/* Botão de upload de cartão */}
          <input
            type="file"
            ref={fileInputRef}
            accept="image/jpeg,image/jpg,image/png,image/webp"
            onChange={handleCardFileSelect}
            className="hidden"
          />
          <Button 
            variant="ghost" 
            size="icon"
            onClick={handleCardUploadClick}
            disabled={isUploadingCard || isUploadingAudio || loading}
            title="Enviar cartão de visita"
          >
            {isUploadingCard ? (
              <Loader2 className="w-5 h-5 animate-spin" />
            ) : (
              <Paperclip className="w-5 h-5" />
            )}
          </Button>
          
          {/* Botão de gravação de áudio */}
          <input
            type="file"
            ref={audioInputRef}
            accept="audio/mpeg,audio/mp3,audio/wav,audio/webm,audio/ogg,audio/m4a"
            onChange={handleAudioFileSelect}
            className="hidden"
          />
          <Button 
            variant="ghost" 
            size="icon"
            onClick={handleMicClick}
            disabled={isUploadingCard || isUploadingAudio || loading}
            className={isRecording ? "bg-destructive/20 text-destructive" : ""}
            title={isRecording ? "Parar gravação" : "Iniciar gravação de áudio"}
          >
            {isRecording ? (
              <StopCircle className="w-5 h-5" />
            ) : (
              <Mic className="w-5 h-5" />
            )}
          </Button>
          {/* Botão de upload de arquivo de áudio */}
          {!isRecording && (
            <Button
              variant="ghost"
              size="icon"
              onClick={() => audioInputRef.current?.click()}
              disabled={isUploadingCard || isUploadingAudio || loading}
              title="Enviar arquivo de áudio"
            >
              <Upload className="w-5 h-5" />
            </Button>
          )}
          
          {/* Mostrar tempo de gravação */}
          {isRecording && (
            <span className="text-xs text-muted-foreground min-w-[3rem]">
              {formatTime(recordingTime)}
            </span>
          )}
          
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={(e) => e.key === "Enter" && handleSend()}
            placeholder="Digite sua mensagem..."
            className="flex-1 bg-secondary rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50"
            disabled={isRecording}
          />
          <Button 
            variant="glow" 
            size="icon" 
            onClick={handleSend}
            disabled={loading || !input.trim() || isRecording}
          >
            {loading ? (
              <Loader2 className="w-5 h-5 animate-spin" />
            ) : (
              <Send className="w-5 h-5" />
            )}
          </Button>
        </div>
      </div>
    </div>
  );
}

