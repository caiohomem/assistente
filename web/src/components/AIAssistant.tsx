"use client"

import { Send, Sparkles, Mic, Paperclip, Loader2 } from "lucide-react";
import { Button } from "./ui/button";
import { useState } from "react";

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

  const formatTimestamp = () => {
    return new Date().toLocaleTimeString("pt-BR", { 
      hour: "2-digit", 
      minute: "2-digit" 
    });
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
              <p className="text-sm">{message.content}</p>
              <p className="text-xs opacity-60 mt-1">{message.timestamp}</p>
            </div>
          </div>
        ))}
      </div>
      
      {/* Input */}
      <div className="p-4 border-t border-border">
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="icon">
            <Paperclip className="w-5 h-5" />
          </Button>
          <Button variant="ghost" size="icon">
            <Mic className="w-5 h-5" />
          </Button>
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyPress={(e) => e.key === "Enter" && handleSend()}
            placeholder="Digite sua mensagem..."
            className="flex-1 bg-secondary rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50"
          />
          <Button 
            variant="glow" 
            size="icon" 
            onClick={handleSend}
            disabled={loading || !input.trim()}
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

