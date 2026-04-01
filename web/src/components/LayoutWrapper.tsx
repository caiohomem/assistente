"use client"

import { ReactNode, useEffect, useState } from "react"
import { Sidebar } from "./Sidebar"
import { Header } from "./Header"
import { Menu } from "lucide-react"
import { Button } from "./ui/button"
import { cn } from "@/lib/utils"

interface LayoutWrapperProps {
  children: ReactNode
  title: string
  subtitle?: string
  activeTab?: string
}

export function LayoutWrapper({ children, title, subtitle, activeTab }: LayoutWrapperProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  useEffect(() => {
    if (typeof document === "undefined") {
      return
    }

    document.body.style.overflow = sidebarOpen ? "hidden" : ""

    return () => {
      document.body.style.overflow = ""
    }
  }, [sidebarOpen])

  return (
    <div className="min-h-screen bg-background overflow-x-hidden">
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
      
      <main className="relative min-h-screen px-4 pb-6 pt-20 lg:ml-64 lg:p-8">
        {/* Mobile menu button */}
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className={cn(
            "lg:hidden fixed left-4 top-4 z-[60] h-11 w-11 rounded-2xl bg-card/85 backdrop-blur-md border border-border/50 shadow-lg shadow-black/20 hover:bg-card transition-opacity duration-300",
            sidebarOpen && "opacity-0 pointer-events-none"
          )}
        >
          <Menu className="w-5 h-5" />
        </Button>
        
        <div className="mx-auto max-w-7xl">
          <Header title={title} subtitle={subtitle} />
          {children}
        </div>
      </main>
    </div>
  )
}
