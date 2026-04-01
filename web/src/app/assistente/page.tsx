"use client"

import { useEffect, useState } from "react"
import { useAuth } from "@clerk/nextjs"
import { useRouter } from "next/navigation"
import { LayoutWrapper } from "@/components/LayoutWrapper"
import { AIAssistant } from "@/components/AIAssistant"

export default function AssistentePage() {
  const { isLoaded, isSignedIn } = useAuth()
  const router = useRouter()
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!isLoaded) {
      return
    }

    if (!isSignedIn) {
      router.replace(`/login?returnUrl=${encodeURIComponent("/assistente")}`)
      return
    }

    let isMounted = true

    async function check() {
      try {
      } finally {
        if (isMounted) setLoading(false)
      }
    }

    check()
    return () => {
      isMounted = false
    }
  }, [isLoaded, isSignedIn, router])

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




