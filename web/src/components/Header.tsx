"use client"

import { Search, Bell, Plus } from "lucide-react"
import { Button } from "./ui/button"
import { useRouter } from "next/navigation"
import { UserMenu } from "./UserMenu"
import { LanguageSelector } from "./LanguageSelector"
import { ThemeSelector } from "./ThemeSelector"

interface HeaderProps {
  title: string
  subtitle?: string
}

export function Header({ title, subtitle }: HeaderProps) {
  const router = useRouter()

  return (
    <header className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4 mb-8 animate-blur-in">
      <div className="lg:flex-1">
        <h1 className="text-2xl lg:text-3xl font-bold bg-gradient-to-r from-foreground via-foreground to-foreground/70 bg-clip-text text-transparent">{title}</h1>
        {subtitle && <p className="text-muted-foreground mt-1.5 text-sm">{subtitle}</p>}
      </div>
      
      <div className="flex items-center gap-2 lg:gap-4 relative z-10">
        <div className="relative group hidden md:block">
          <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground transition-colors duration-300 group-focus-within:text-primary" />
          <input
            type="text"
            placeholder="Buscar contatos, notas..."
            className="w-48 lg:w-72 bg-secondary/50 backdrop-blur-sm border border-border/50 rounded-2xl pl-11 pr-5 py-3 text-sm transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50 focus:bg-secondary/80 lg:focus:w-80 placeholder:text-muted-foreground/70"
          />
          {/* Search glow */}
          <div className="absolute inset-0 rounded-2xl bg-primary/5 opacity-0 group-focus-within:opacity-100 transition-opacity duration-300 -z-10 blur-xl" />
        </div>
        
        <Button variant="glass" size="icon" className="relative group h-10 w-10 lg:h-12 lg:w-12 rounded-2xl transition-all duration-300 hover:bg-secondary/80 hover:scale-105">
          <Bell className="w-4 h-4 lg:w-5 lg:h-5 transition-transform duration-300 group-hover:rotate-12" />
          <span className="absolute top-1.5 right-1.5 lg:top-2 lg:right-2 w-2 h-2 lg:w-2.5 lg:h-2.5 bg-primary rounded-full animate-pulse ring-2 lg:ring-4 ring-primary/20" />
        </Button>
        
        <Button 
          variant="glow" 
          className="h-10 lg:h-12 px-3 lg:px-5 rounded-2xl text-xs lg:text-sm font-medium transition-all duration-300 hover:scale-105 hover:shadow-lg hover:shadow-primary/30"
          onClick={() => router.push("/contatos?novo=true")}
        >
          <Plus className="w-3 h-3 lg:w-4 lg:h-4 lg:mr-2 transition-transform duration-300 group-hover:rotate-90" />
          <span className="hidden lg:inline">Novo Contato</span>
          <span className="lg:hidden">Novo</span>
        </Button>
        
        <div className="hidden lg:flex items-center gap-2">
          <LanguageSelector />
          <ThemeSelector />
        </div>
        
        <UserMenu />
      </div>
    </header>
  )
}

