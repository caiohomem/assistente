"use client"

import { useState, useEffect } from "react"
import {
  LayoutDashboard,
  Users,
  CreditCard,
  Mic,
  FileText,
  Network,
  MessageSquare,
  Settings,
  Sparkles,
  Menu,
  X,
  Bell,
  Coins,
  Building2,
  Zap,
} from "lucide-react"
import { cn } from "@/lib/utils"
import { usePathname, useRouter } from "next/navigation"

interface SidebarProps {
  activeTab?: string
  onTabChange?: (tab: string) => void
  isOpen?: boolean
  onToggle?: () => void
}

const menuItems = [
  { id: "dashboard", label: "Dashboard", icon: LayoutDashboard, path: "/dashboard" },
  { id: "contacts", label: "Contatos", icon: Users, path: "/contatos" },
  { id: "companies", label: "Empresas", icon: Building2, path: "/empresas" },
  { id: "cards", label: "Cartões", icon: CreditCard, path: "/contatos/upload-cartao" },
  { id: "notes", label: "Notas", icon: Mic, path: "/contatos/notas" },
  { id: "documents", label: "Documentos", icon: FileText, path: "/documentos" },
  { id: "reminders", label: "Lembretes", icon: Bell, path: "/automacao/lembretes" },
  { id: "workflows", label: "Workflows", icon: Zap, path: "/workflows" },
  { id: "network", label: "Rede", icon: Network, path: "/contatos/rede" },
  { id: "assistant", label: "Assistente IA", icon: Sparkles, path: "/assistente" },
  { id: "credits", label: "Créditos", icon: Coins, path: "/creditos" },
]

export function Sidebar({ activeTab, onTabChange, isOpen, onToggle }: SidebarProps) {
  const pathname = usePathname()
  const router = useRouter()

  const handleTabChange = (item: typeof menuItems[0]) => {
    if (onTabChange) {
      onTabChange(item.id)
    } else {
      router.push(item.path)
    }
    // Fechar menu mobile após seleção
    if (onToggle && window.innerWidth < 1024) {
      onToggle()
    }
  }

  const getActiveTab = () => {
    if (activeTab) return activeTab
    // Find the most specific match (longest path that matches)
    const matchingItems = menuItems.filter(item => pathname?.startsWith(item.path))
    if (matchingItems.length === 0) return "dashboard"
    // Sort by path length descending to get the most specific match
    const mostSpecific = matchingItems.sort((a, b) => b.path.length - a.path.length)[0]
    return mostSpecific.id
  }

  const currentActiveTab = getActiveTab()

  return (
    <>
      {/* Mobile overlay */}
      {isOpen && (
        <div 
          className="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 lg:hidden"
          onClick={onToggle}
        />
      )}
      
      {/* Sidebar */}
      <aside className={cn(
        "fixed left-0 top-0 h-screen w-64 bg-card/80 backdrop-blur-xl border-r border-border/50 flex flex-col z-50 transition-transform duration-300 ease-in-out",
        "lg:translate-x-0",
        isOpen ? "translate-x-0" : "-translate-x-full lg:translate-x-0"
      )}>
      {/* Background glow */}
      <div className="absolute inset-0 bg-gradient-to-b from-primary/5 via-transparent to-accent/5 pointer-events-none" />
      
      {/* Logo */}
      <div className="p-6 border-b border-border/50 relative flex items-center justify-between">
        <div className="flex items-center gap-3 group cursor-pointer" onClick={() => router.push("/dashboard")}>
          <div className="w-11 h-11 rounded-xl bg-gradient-to-br from-primary to-accent flex items-center justify-center shadow-lg shadow-primary/20 transition-all duration-500 group-hover:shadow-primary/40 group-hover:scale-105">
            <Sparkles className="w-5 h-5 text-primary-foreground transition-transform duration-500 group-hover:rotate-12" />
          </div>
          <div>
            <h1 className="font-semibold text-lg gradient-text">Executive AI</h1>
            <p className="text-xs text-muted-foreground">Assistente Executivo</p>
          </div>
        </div>
        {/* Mobile close button */}
        <button
          onClick={onToggle}
          className="lg:hidden p-2 rounded-lg hover:bg-secondary/80 transition-colors"
        >
          <X className="w-5 h-5 text-foreground" />
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-4 space-y-1 relative overflow-y-auto">
        {menuItems.map((item, index) => {
          const Icon = item.icon
          const isActive = currentActiveTab === item.id
          
          return (
            <button
              key={item.id}
              onClick={() => handleTabChange(item)}
              className={cn(
                "w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all duration-300 relative overflow-hidden group",
                isActive 
                  ? "bg-primary/15 text-primary shadow-lg shadow-primary/10" 
                  : "text-muted-foreground hover:text-foreground hover:bg-secondary/80"
              )}
              style={{ 
                animationDelay: `${index * 50}ms`,
                animation: 'slideUp 0.5s cubic-bezier(0.16, 1, 0.3, 1) forwards'
              }}
            >
              {/* Active indicator */}
              {isActive && (
                <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-8 bg-gradient-to-b from-primary to-accent rounded-r-full" />
              )}
              
              {/* Hover glow effect */}
              <div className={cn(
                "absolute inset-0 opacity-0 transition-opacity duration-500",
                "bg-gradient-to-r from-primary/10 to-transparent",
                !isActive && "group-hover:opacity-100"
              )} />
              
              <Icon className={cn(
                "w-5 h-5 transition-all duration-300 relative z-10",
                isActive ? "text-primary" : "group-hover:text-primary group-hover:scale-110"
              )} />
              <span className="relative z-10">{item.label}</span>
              
              {/* Arrow indicator on hover */}
              {!isActive && (
                <div className="ml-auto opacity-0 -translate-x-2 transition-all duration-300 group-hover:opacity-100 group-hover:translate-x-0">
                  <div className="w-1.5 h-1.5 rounded-full bg-primary" />
                </div>
              )}
            </button>
          )
        })}
      </nav>

      {/* Settings */}
      <div className="p-4 border-t border-border/50 relative">
        <button 
          onClick={() => router.push("/configuracoes")}
          className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium text-muted-foreground hover:text-foreground hover:bg-secondary/80 transition-all duration-300 group"
        >
          <Settings className="w-5 h-5 transition-transform duration-500 group-hover:rotate-90" />
          <span>Configurações</span>
        </button>
      </div>
      </aside>
    </>
  )
}

