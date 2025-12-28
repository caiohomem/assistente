"use client"

import { useState, useEffect } from "react"
import { useSearchParams, useRouter } from "next/navigation"
import { LayoutWrapper } from "@/components/LayoutWrapper"
import { Button } from "@/components/ui/button"
import { FileText, FileEdit, Stamp } from "lucide-react"
import { cn } from "@/lib/utils"
import { DraftsListClient } from "@/components/DraftsListClient"
import { TemplatesListClient } from "@/components/TemplatesListClient"
import { LetterheadsListClient } from "@/components/LetterheadsListClient"

type TabType = "drafts" | "templates" | "letterheads"

export default function DocumentosPage() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const tabParam = searchParams.get("tab") as TabType | null
  const [activeTab, setActiveTab] = useState<TabType>(tabParam || "drafts")
  
  useEffect(() => {
    if (tabParam && ["drafts", "templates", "letterheads"].includes(tabParam)) {
      setActiveTab(tabParam)
    }
  }, [tabParam])
  
  const handleTabChange = (tab: TabType) => {
    setActiveTab(tab)
    const params = new URLSearchParams(searchParams.toString())
    params.set("tab", tab)
    if (params.get("novo") === "true") {
      params.delete("novo")
    }
    router.push(`/documentos?${params.toString()}`)
  }

  return (
    <LayoutWrapper 
      title="Documentos" 
      subtitle="Gerencie rascunhos, templates e papéis timbrados" 
      activeTab="documents"
    >
      <div className="space-y-6">
        {/* Tabs */}
        <div className="flex gap-2 border-b border-border overflow-x-auto">
          <button
            onClick={() => handleTabChange("drafts")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "drafts"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <FileEdit className="w-4 h-4" />
              Rascunhos
            </div>
          </button>
          <button
            onClick={() => handleTabChange("templates")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "templates"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <FileText className="w-4 h-4" />
              Templates
            </div>
          </button>
          <button
            onClick={() => handleTabChange("letterheads")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "letterheads"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <Stamp className="w-4 h-4" />
              Papéis Timbrados
            </div>
          </button>
        </div>

        {/* Content */}
        {activeTab === "drafts" && <DraftsListClient />}
        {activeTab === "templates" && <TemplatesListClient />}
        {activeTab === "letterheads" && <LetterheadsListClient />}
      </div>
    </LayoutWrapper>
  )
}

