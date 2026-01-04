"use client"

import { LayoutWrapper } from "@/components/LayoutWrapper"
import { AIAssistant } from "@/components/AIAssistant"

export default function AssistentePage() {
  return (
    <LayoutWrapper title="Assistente IA" subtitle="Seu assistente executivo inteligente" activeTab="assistant">
      <div className="max-w-4xl mx-auto">
        <AIAssistant />
      </div>
    </LayoutWrapper>
  )
}
