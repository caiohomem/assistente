"use client"

import { Mail, Phone, Link as LinkIcon, Building2, Mic, Users } from "lucide-react"
import { Button } from "./ui/button"
import { Contact } from "@/lib/types/contact"
import { cn } from "@/lib/utils"
import { useRouter } from "next/navigation"

interface ContactCardProps {
  contact: Contact
  delay?: number
}

export function ContactCard({ contact, delay = 0 }: ContactCardProps) {
  const router = useRouter()
  
  const getInitials = (name: string) => {
    return name
      .split(" ")
      .map(n => n[0])
      .join("")
      .toUpperCase()
      .slice(0, 2)
  }

  const getLastContact = () => {
    const updated = new Date(contact.updatedAt)
    const now = new Date()
    const diffMs = now.getTime() - updated.getTime()
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))
    
    if (diffDays === 0) {
      const diffHours = Math.floor(diffMs / (1000 * 60 * 60))
      if (diffHours === 0) {
        const diffMins = Math.floor(diffMs / (1000 * 60))
        return `Hoje, ${updated.getHours().toString().padStart(2, '0')}:${updated.getMinutes().toString().padStart(2, '0')}`
      }
      return "Hoje"
    } else if (diffDays === 1) {
      return "Ontem"
    } else if (diffDays < 7) {
      return `${diffDays} dias atrás`
    } else {
      return "Semana passada"
    }
  }

  return (
    <div 
      className="glass-card p-5 card-hover animate-slide-up cursor-pointer"
      style={{ animationDelay: `${delay}ms` }}
      onClick={() => router.push(`/contatos/${contact.contactId}`)}
    >
      <div className="flex items-start gap-4">
        {/* Avatar */}
        <div className="relative shrink-0">
          <div className="w-14 h-14 rounded-full bg-gradient-to-br from-primary to-accent flex items-center justify-center text-primary-foreground font-semibold text-lg">
            {getInitials(contact.fullName)}
          </div>
          <div className="absolute bottom-0 right-0 w-4 h-4 bg-green-500 rounded-full border-2 border-card" />
        </div>
        
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-foreground truncate">{contact.fullName}</h3>
          
          {contact.jobTitle && (
            <p className="text-sm text-muted-foreground mt-1">{contact.jobTitle}</p>
          )}
          
          {contact.company && (
            <div className="flex items-center gap-1 mt-1 text-sm text-muted-foreground">
              <Building2 className="w-3 h-3" />
              <span className="truncate">{contact.company}</span>
            </div>
          )}
          
          <div className="flex items-center gap-2 mt-3">
            {contact.emails.length > 0 && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 rounded-lg"
                onClick={(e) => {
                  e.stopPropagation()
                  window.location.href = `mailto:${contact.emails[0]}`
                }}
                title="Enviar e-mail"
              >
                <Mail className="w-4 h-4" />
              </Button>
            )}
            
            {contact.phones.length > 0 && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 rounded-lg"
                onClick={(e) => {
                  e.stopPropagation()
                  window.location.href = `tel:${contact.phones[0]}`
                }}
                title="Ligar"
              >
                <Phone className="w-4 h-4" />
              </Button>
            )}

            {contact.phones.length > 0 && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 rounded-lg text-green-500 hover:text-green-600 hover:bg-green-500/10"
                onClick={(e) => {
                  e.stopPropagation()
                  const phone = contact.phones[0].replace(/\D/g, '')
                  window.open(`https://wa.me/${phone}`, '_blank')
                }}
                title="Abrir WhatsApp"
              >
                <svg 
                  className="w-4 h-4" 
                  fill="currentColor" 
                  viewBox="0 0 24 24"
                  xmlns="http://www.w3.org/2000/svg"
                >
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 01-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 01-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 012.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0012.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 005.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 00-3.48-8.413Z"/>
                </svg>
              </Button>
            )}
            
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 rounded-lg"
              onClick={(e) => {
                e.stopPropagation()
                router.push(`/contatos/${contact.contactId}/notas-audio`)
              }}
              title="Adicionar nota de áudio"
            >
              <Mic className="w-4 h-4" />
            </Button>

            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 rounded-lg"
              onClick={(e) => {
                e.stopPropagation()
                router.push(`/contatos/${contact.contactId}/relacionamentos/novo`)
              }}
              title="Adicionar relacionamento"
            >
              <Users className="w-4 h-4" />
            </Button>
            
            {contact.relationships.length > 0 && (
              <div className="flex items-center gap-1 ml-auto text-xs text-muted-foreground">
                <LinkIcon className="w-3 h-3" />
                <span>{contact.relationships.length}</span>
              </div>
            )}
          </div>
          
          <p className="text-xs text-muted-foreground mt-3 flex items-center gap-1">
            <span className="w-2 h-2 bg-green-500 rounded-full" />
            Último contato: {getLastContact()}
          </p>
        </div>
      </div>
    </div>
  )
}

