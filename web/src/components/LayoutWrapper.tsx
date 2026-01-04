"use client"

import { ReactNode, useEffect, useState } from "react"
import { Sidebar } from "./Sidebar"
import { Header } from "./Header"
import { Menu } from "lucide-react"
import { Button } from "./ui/button"
import { cn } from "@/lib/utils"
import { getBffSession } from "@/lib/bff"

interface LayoutWrapperProps {
  children: ReactNode
  title: string
  subtitle?: string
  activeTab?: string
}

export function LayoutWrapper({ children, title, subtitle, activeTab }: LayoutWrapperProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  useEffect(() => {
    let mounted = true

    async function ensureAuthenticated() {
      try {
        const session = await getBffSession()
        if (!mounted) return
        if (!session.authenticated) {
          const currentPath = window.location.pathname + window.location.search
          window.location.href = `/login?returnUrl=${encodeURIComponent(currentPath)}`
        }
      } catch {
        if (!mounted) return
        const currentPath = window.location.pathname + window.location.search
        window.location.href = `/login?returnUrl=${encodeURIComponent(currentPath)}`
      }
    }

    void ensureAuthenticated()

    return () => {
      mounted = false
    }
  }, [])

  return (
    <div className="min-h-screen bg-background overflow-hidden">
      {/* Animated background */}
      <div className="fixed inset-0 pointer-events-none">
        {/* Primary gradient */}
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_hsl(199_89%_48%_/_0.08),_transparent_50%)]" />
        
        {/* Accent gradient */}
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_bottom_right,_hsl(280_100%_70%_/_0.05),_transparent_50%)]" />
        
        {/* Animated orbs */}
        <div className="absolute top-1/4 left-1/4 w-96 h-96 bg-primary/5 rounded-full blur-3xl animate-float" />
        <div className="absolute bottom-1/4 right-1/4 w-80 h-80 bg-accent/5 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }} />
        
        {/* Grid pattern overlay */}
        <div 
          className="absolute inset-0 opacity-[0.02]"
          style={{
            backgroundImage: `linear-gradient(hsl(var(--foreground)) 1px, transparent 1px),
                             linear-gradient(90deg, hsl(var(--foreground)) 1px, transparent 1px)`,
            backgroundSize: '60px 60px'
          }}
        />
      </div>
      
      <Sidebar activeTab={activeTab} isOpen={sidebarOpen} onToggle={() => setSidebarOpen(!sidebarOpen)} />
      
      <main className="lg:ml-64 p-4 lg:p-8 relative min-h-screen">
        {/* Mobile menu button */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className={cn(
            "lg:hidden fixed top-4 left-4 z-50 bg-card/80 backdrop-blur-sm border border-border/50 hover:bg-card transition-opacity duration-300",
            sidebarOpen && "opacity-0 pointer-events-none"
          )}
        >
          <Menu className="w-5 h-5" />
        </Button>
        
        <div className="max-w-7xl mx-auto pt-12 lg:pt-0">
          <Header title={title} subtitle={subtitle} />
          {children}
        </div>
      </main>
    </div>
  )
}
