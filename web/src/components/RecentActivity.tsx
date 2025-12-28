"use client"

import { CreditCard, Mic, FileText, UserPlus, MessageSquare } from "lucide-react"
import { cn } from "@/lib/utils"

interface Activity {
  id: string
  type: "card" | "audio" | "document" | "contact" | "ai"
  title: string
  description: string
  time: string
}

const activities: Activity[] = [
  { id: "1", type: "card", title: "Novo cartão escaneado", description: "Ana Silva - TechCorp", time: "5 min" },
  { id: "2", type: "audio", title: "Nota de áudio gravada", description: "Reunião com investidores", time: "1 hora" },
  { id: "3", type: "contact", title: "Novo contato adicionado", description: "Carlos Mendes via LinkedIn", time: "2 horas" },
  { id: "4", type: "ai", title: "Análise de relacionamento", description: "3 novas conexões identificadas", time: "3 horas" },
  { id: "5", type: "document", title: "Documento anexado", description: "Proposta comercial.pdf", time: "5 horas" },
]

const iconMap = {
  card: CreditCard,
  audio: Mic,
  document: FileText,
  contact: UserPlus,
  ai: MessageSquare,
}

const colorMap = {
  card: "text-primary bg-primary/10",
  audio: "text-accent bg-accent/10",
  document: "text-warning bg-warning/10",
  contact: "text-success bg-success/10",
  ai: "text-primary bg-primary/10",
}

export function RecentActivity() {
  return (
    <div 
      className="glass-card p-6 relative overflow-hidden animate-slide-up"
      style={{ animationDelay: "200ms" }}
    >
      {/* Header with gradient line */}
      <div className="flex items-center justify-between mb-5">
        <h2 className="font-semibold text-lg">Atividade Recente</h2>
        <div className="h-1 w-12 bg-gradient-to-r from-primary to-accent rounded-full" />
      </div>
      
      <div className="space-y-2">
        {activities.map((activity, index) => {
          const Icon = iconMap[activity.type]
          const colorClass = colorMap[activity.type]
          
          return (
            <div 
              key={activity.id}
              className="flex items-start gap-3 p-3 rounded-xl hover:bg-secondary/50 transition-all duration-300 cursor-pointer group relative overflow-hidden"
              style={{ 
                animationDelay: `${(index + 4) * 80}ms`,
                animation: 'slideUp 0.5s cubic-bezier(0.16, 1, 0.3, 1) forwards',
                opacity: 0
              }}
            >
              {/* Hover highlight */}
              <div className="absolute inset-0 bg-gradient-to-r from-primary/5 to-transparent opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
              
              <div className={cn(
                "w-10 h-10 rounded-xl flex items-center justify-center shrink-0 transition-all duration-300 group-hover:scale-110",
                colorClass
              )}>
                <Icon className="w-5 h-5" />
              </div>
              <div className="flex-1 min-w-0 relative z-10">
                <p className="font-medium text-sm transition-colors duration-300 group-hover:text-foreground">{activity.title}</p>
                <p className="text-xs text-muted-foreground truncate">{activity.description}</p>
              </div>
              <span className="text-xs text-muted-foreground shrink-0 bg-secondary/50 px-2 py-1 rounded-full transition-all duration-300 group-hover:bg-primary/10 group-hover:text-primary">{activity.time}</span>
            </div>
          )
        })}
      </div>
    </div>
  )
}

