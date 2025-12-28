"use client"

import { Camera, Upload, Sparkles } from "lucide-react"
import { Button } from "./ui/button"
import { useState } from "react"
import { useRouter } from "next/navigation"

interface BusinessCardScannerProps {
  onFileSelect?: (file: File) => void
}

export function BusinessCardScanner({ onFileSelect }: BusinessCardScannerProps) {
  const router = useRouter()
  const [isDragging, setIsDragging] = useState(false)

  const handleFileUpload = (file: File) => {
    if (onFileSelect) {
      onFileSelect(file)
    } else {
      // Redirecionar para a página de upload de cartão
      router.push("/contatos/upload-cartao")
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setIsDragging(false)
    
    const file = e.dataTransfer.files[0]
    if (file && file.type.startsWith("image/")) {
      handleFileUpload(file)
    }
  }

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      handleFileUpload(file)
    }
  }

  return (
    <div className="glass-card p-6 animate-slide-up">
      {/* Header */}
      <div className="flex items-center gap-3 mb-4">
        <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary to-accent flex items-center justify-center">
          <Sparkles className="w-5 h-5 text-primary-foreground" />
        </div>
        <div>
          <h2 className="font-semibold text-lg">Scanner de Cartões</h2>
          <p className="text-xs text-muted-foreground">IA Vision para extração automática</p>
        </div>
      </div>

      {/* Drop Zone */}
      <div
        className={`
          border-2 border-dashed rounded-2xl p-8 text-center transition-all duration-300
          ${isDragging 
            ? "border-primary bg-primary/10" 
            : "border-border hover:border-primary/50 hover:bg-primary/5"
          }
        `}
        onDrop={handleDrop}
        onDragOver={(e) => {
          e.preventDefault()
          setIsDragging(true)
        }}
        onDragLeave={() => setIsDragging(false)}
      >
        <Camera className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
        <p className="text-sm text-muted-foreground mb-4">
          Arraste uma imagem ou tire uma foto do cartão de visita
        </p>
        
        <div className="flex items-center justify-center gap-3">
          <label className="cursor-pointer">
            <input
              type="file"
              accept="image/*"
              capture="environment"
              onChange={handleFileInput}
              className="hidden"
            />
            <Button variant="glass" className="gap-2 pointer-events-none">
              <Camera className="w-4 h-4" />
              Tirar Foto
            </Button>
          </label>
          
          <label className="cursor-pointer">
            <input
              type="file"
              accept="image/*"
              onChange={handleFileInput}
              className="hidden"
            />
            <Button variant="glass" className="gap-2 pointer-events-none">
              <Upload className="w-4 h-4" />
              Upload
            </Button>
          </label>
        </div>
      </div>

      {/* Description */}
      <p className="text-xs text-primary/80 mt-4 text-center">
        GPT Vision extrai automaticamente: nome, cargo, empresa, email, telefone e redes sociais
      </p>
    </div>
  )
}

