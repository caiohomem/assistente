"use client"

import { useEffect, useState } from "react"
import { getBffSession } from "@/lib/bff"
import { LayoutWrapper } from "@/components/LayoutWrapper"
import { AIAssistant } from "@/components/AIAssistant"

export default function AssistentePage() {
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let isMounted = true

    async function check() {
      try {
        const session = await getBffSession()
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/assistente")}`
          return
        }
      } finally {
        if (isMounted) setLoading(false)
      }
    }

    check()
    return () => {
      isMounted = false
    }
  }, [])

  if (loading) {
    return (
      <LayoutWrapper title="Assistente IA" subtitle="Seu assistente executivo inteligente" activeTab="assistant">
        <div className="flex items-center justify-center py-12">
          <p className="text-muted-foreground">Carregando...</p>
        </div>
      </LayoutWrapper>
    )
  }

  return (
    <LayoutWrapper title="Assistente IA" subtitle="Seu assistente executivo inteligente" activeTab="assistant">
      <div className="max-w-4xl mx-auto">
        <AIAssistant />
      </div>
    </LayoutWrapper>
  )
}





