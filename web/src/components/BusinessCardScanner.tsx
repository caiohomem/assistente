"use client"

import { Camera, Upload, Sparkles, Loader2, Check, X, ExternalLink, RotateCcw } from "lucide-react"
import { Button } from "./ui/button"
import { useState, useRef } from "react"
import { useRouter } from "next/navigation"
import { uploadCard, getCaptureJobById } from "@/lib/api/captureApi"
import type { UploadCardResponse } from "@/lib/api/captureApi"
import type { CardScanResult } from "@/lib/types/capture"

interface BusinessCardScannerProps {
  onFileSelect?: (file: File) => void
  compact?: boolean
}

type ScanState = "idle" | "uploading" | "processing" | "success" | "error"

export function BusinessCardScanner({ onFileSelect, compact = false }: BusinessCardScannerProps) {
  const router = useRouter()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const cameraInputRef = useRef<HTMLInputElement>(null)

  const [isDragging, setIsDragging] = useState(false)
  const [scanState, setScanState] = useState<ScanState>("idle")
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [ocrResult, setOcrResult] = useState<CardScanResult | null>(null)
  const [contactId, setContactId] = useState<string | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const resetScanner = () => {
    setScanState("idle")
    setPreviewUrl(null)
    setOcrResult(null)
    setContactId(null)
    setErrorMessage(null)
    if (fileInputRef.current) fileInputRef.current.value = ""
    if (cameraInputRef.current) cameraInputRef.current.value = ""
  }

  const processCard = async (file: File) => {
    // Validar tipo de arquivo
    const allowedTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp"]
    if (!allowedTypes.includes(file.type)) {
      setErrorMessage("Tipo de arquivo não suportado. Use JPEG, PNG ou WebP.")
      setScanState("error")
      return
    }

    // Validar tamanho (máximo 10MB)
    const maxSize = 10 * 1024 * 1024
    if (file.size > maxSize) {
      setErrorMessage("Arquivo muito grande. Tamanho máximo: 10MB.")
      setScanState("error")
      return
    }

    // Criar preview
    const reader = new FileReader()
    reader.onloadend = () => {
      setPreviewUrl(reader.result as string)
    }
    reader.readAsDataURL(file)

    setScanState("uploading")
    setErrorMessage(null)

    try {
      // Upload do cartão
      const result: UploadCardResponse = await uploadCard({ file })
      setContactId(result.contactId)

      setScanState("processing")

      // Aguardar processamento OCR
      await new Promise(resolve => setTimeout(resolve, 1500))

      const job = await getCaptureJobById(result.jobId)

      if (job.cardScanResult) {
        setOcrResult(job.cardScanResult)
        setScanState("success")
      } else {
        setErrorMessage("Não foi possível extrair dados do cartão.")
        setScanState("error")
      }
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Erro ao processar cartão")
      setScanState("error")
    }
  }

  const handleFileUpload = (file: File) => {
    if (onFileSelect) {
      // Se tem callback externo, usar ele
      onFileSelect(file)
    } else {
      // Processar diretamente no componente
      processCard(file)
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

  // Renderizar estado de sucesso
  if (scanState === "success" && ocrResult) {
    return (
      <div className="glass-card p-6 animate-slide-up">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-full bg-green-500/20 flex items-center justify-center">
              <Check className="w-4 h-4 text-green-500" />
            </div>
            <span className="font-semibold text-green-500">Cartão Processado!</span>
          </div>
          <Button variant="ghost" size="icon" onClick={resetScanner} title="Escanear outro">
            <RotateCcw className="w-4 h-4" />
          </Button>
        </div>

        {/* Preview da imagem */}
        {previewUrl && (
          <div className="mb-4 rounded-xl overflow-hidden border border-border">
            <img src={previewUrl} alt="Cartão" className="w-full h-32 object-cover" />
          </div>
        )}

        {/* Dados extraídos */}
        <div className="space-y-2 text-sm">
          {ocrResult.name && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Nome:</span>
              <span className="font-medium">{ocrResult.name}</span>
            </div>
          )}
          {ocrResult.company && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Empresa:</span>
              <span className="font-medium">{ocrResult.company}</span>
            </div>
          )}
          {ocrResult.jobTitle && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Cargo:</span>
              <span className="font-medium">{ocrResult.jobTitle}</span>
            </div>
          )}
          {ocrResult.email && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Email:</span>
              <span className="font-medium truncate ml-2">{ocrResult.email}</span>
            </div>
          )}
          {ocrResult.phone && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Telefone:</span>
              <span className="font-medium">{ocrResult.phone}</span>
            </div>
          )}
        </div>

        {/* Ações */}
        <div className="flex gap-2 mt-4">
          {contactId && (
            <Button
              variant="glow"
              className="flex-1 gap-2"
              onClick={() => router.push(`/contatos/${contactId}`)}
            >
              <ExternalLink className="w-4 h-4" />
              Ver Contato
            </Button>
          )}
          <Button variant="glass" onClick={resetScanner}>
            Novo Scan
          </Button>
        </div>
      </div>
    )
  }

  // Renderizar estado de erro
  if (scanState === "error") {
    return (
      <div className="glass-card p-6 animate-slide-up">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-full bg-destructive/20 flex items-center justify-center">
              <X className="w-4 h-4 text-destructive" />
            </div>
            <span className="font-semibold text-destructive">Erro</span>
          </div>
          <Button variant="ghost" size="icon" onClick={resetScanner}>
            <RotateCcw className="w-4 h-4" />
          </Button>
        </div>

        {previewUrl && (
          <div className="mb-4 rounded-xl overflow-hidden border border-destructive/50">
            <img src={previewUrl} alt="Cartão" className="w-full h-32 object-cover opacity-50" />
          </div>
        )}

        <p className="text-sm text-destructive mb-4">{errorMessage}</p>

        <Button variant="glass" className="w-full" onClick={resetScanner}>
          Tentar Novamente
        </Button>
      </div>
    )
  }

  // Renderizar estado de processamento
  if (scanState === "uploading" || scanState === "processing") {
    return (
      <div className="glass-card p-6 animate-slide-up">
        <div className="flex items-center gap-3 mb-4">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-primary to-accent flex items-center justify-center">
            <Loader2 className="w-5 h-5 text-primary-foreground animate-spin" />
          </div>
          <div>
            <h2 className="font-semibold text-lg">
              {scanState === "uploading" ? "Enviando..." : "Processando OCR..."}
            </h2>
            <p className="text-xs text-muted-foreground">
              {scanState === "uploading" ? "Fazendo upload da imagem" : "Extraindo dados com IA Vision"}
            </p>
          </div>
        </div>

        {previewUrl && (
          <div className="rounded-xl overflow-hidden border border-border">
            <img src={previewUrl} alt="Processando" className="w-full h-40 object-cover" />
            <div className="absolute inset-0 bg-background/50 backdrop-blur-sm flex items-center justify-center">
              <Loader2 className="w-8 h-8 animate-spin text-primary" />
            </div>
          </div>
        )}

        <div className="mt-4 h-2 bg-secondary rounded-full overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-primary to-accent animate-pulse"
            style={{ width: scanState === "uploading" ? "40%" : "80%" }}
          />
        </div>
      </div>
    )
  }

  // Renderizar estado idle (padrão)
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
              ref={cameraInputRef}
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
              ref={fileInputRef}
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

