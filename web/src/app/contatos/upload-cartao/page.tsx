"use client";

import { useState, useRef } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { uploadCard, getCaptureJobById, type UploadCardResponse } from "@/lib/api/captureApi";
import { updateContactClient, getContactByIdClient } from "@/lib/api/contactsApiClient";
import type { CaptureJob, CardScanResult } from "@/lib/types/capture";
import type { Contact } from "@/lib/types/contact";
import { TopBar } from "@/components/TopBar";

export default function UploadCartaoPage() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);
  
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  
  const [uploadResult, setUploadResult] = useState<UploadCardResponse | null>(null);
  const [ocrResult, setOcrResult] = useState<CardScanResult | null>(null);
  const [contact, setContact] = useState<Contact | null>(null);
  
  // Form fields for editing OCR result
  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    phone: "",
    company: "",
    jobTitle: "",
  });

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp"];
    if (!allowedTypes.includes(file.type)) {
      setError("Tipo de arquivo não suportado. Use JPEG, PNG ou WebP.");
      return;
    }

    // Validate file size (10MB)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      setError("Arquivo muito grande. Tamanho máximo: 10MB.");
      return;
    }

    setSelectedFile(file);
    setError(null);
    setSuccess(null);

    // Create preview
    const reader = new FileReader();
    reader.onloadend = () => {
      setPreviewUrl(reader.result as string);
    };
    reader.readAsDataURL(file);
  };

  const handleUpload = async () => {
    if (!selectedFile) {
      setError("Selecione um arquivo primeiro.");
      return;
    }

    setUploading(true);
    setError(null);
    setSuccess(null);

    try {
      const result = await uploadCard({ file: selectedFile });
      setUploadResult(result);
      
      // Fetch the job to get OCR result
      setProcessing(true);
      const job = await getCaptureJobById(result.jobId);
      
      if (job.cardScanResult) {
        setOcrResult(job.cardScanResult);
        
        // Populate form with OCR data
        const nameParts = job.cardScanResult.name?.split(" ", 2) || [];
        setFormData({
          firstName: nameParts[0] || "",
          lastName: nameParts[1] || "",
          email: job.cardScanResult.email || "",
          phone: job.cardScanResult.phone || "",
          company: job.cardScanResult.company || "",
          jobTitle: job.cardScanResult.jobTitle || "",
        });
      }

      // Fetch the created contact
      const createdContact = await getContactByIdClient(result.contactId);
      setContact(createdContact);
      
      setSuccess("Cartão processado com sucesso! Revise os dados abaixo.");
    } catch (err: any) {
      setError(err.message || "Erro ao processar cartão.");
    } finally {
      setUploading(false);
      setProcessing(false);
    }
  };

  const handleSave = async () => {
    if (!contact) {
      setError("Contato não encontrado.");
      return;
    }

    setUploading(true);
    setError(null);

    try {
      // Update contact with edited data
      await updateContactClient(contact.contactId, {
        firstName: formData.firstName || undefined,
        lastName: formData.lastName || undefined,
        jobTitle: formData.jobTitle || undefined,
        company: formData.company || undefined,
      });

      // If email changed and is not empty, add it (backend will handle duplicates)
      if (formData.email && formData.email !== contact.emails[0]) {
        // Note: We'd need an addEmail endpoint call here, but for now we'll just update
        // The backend already added the email from OCR, so this is mainly for editing
      }

      // If phone changed and is not empty, add it (backend will handle duplicates)
      if (formData.phone && formData.phone !== contact.phones[0]) {
        // Note: We'd need an addPhone endpoint call here, but for now we'll just update
        // The backend already added the phone from OCR, so this is mainly for editing
      }

      setSuccess("Contato atualizado com sucesso!");
      
      // Redirect to contact details after a short delay
      setTimeout(() => {
        router.push(`/contatos/${contact.contactId}`);
      }, 1500);
    } catch (err: any) {
      setError(err.message || "Erro ao atualizar contato.");
    } finally {
      setUploading(false);
    }
  };

  const handleReset = () => {
    setSelectedFile(null);
    setPreviewUrl(null);
    setUploadResult(null);
    setOcrResult(null);
    setContact(null);
    setFormData({
      firstName: "",
      lastName: "",
      email: "",
      phone: "",
      company: "",
      jobTitle: "",
    });
    setError(null);
    setSuccess(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
      <TopBar title="Upload de Cartão de Visita" showBackButton backHref="/dashboard" />
      <div className="mx-auto max-w-4xl px-6 py-12">
        <div className="mb-8">
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            Faça upload de uma imagem do cartão de visita para extrair automaticamente as informações usando OCR.
          </p>
        </div>

        {/* Error/Success Messages */}
        {error && (
          <div className="mb-6 rounded-md bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-4">
            <p className="text-sm text-red-800 dark:text-red-400">{error}</p>
          </div>
        )}

        {success && (
          <div className="mb-6 rounded-md bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 p-4">
            <p className="text-sm text-green-800 dark:text-green-400">{success}</p>
          </div>
        )}

        {/* Upload Section */}
        {!uploadResult && (
          <div className="bg-white dark:bg-zinc-800 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-700 p-6 mb-6">
            <h2 className="text-xl font-semibold mb-4 dark:text-zinc-100">Selecionar Imagem</h2>
            
            <div className="space-y-4">
              {/* File Input */}
              <div>
                <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
                  Arquivo de Imagem
                </label>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/jpeg,image/jpg,image/png,image/webp"
                  onChange={handleFileSelect}
                  className="block w-full text-sm text-zinc-500 dark:text-zinc-400
                    file:mr-4 file:py-2 file:px-4
                    file:rounded-md file:border-0
                    file:text-sm file:font-semibold
                    file:bg-zinc-100 file:text-zinc-700
                    dark:file:bg-zinc-700 dark:file:text-zinc-200
                    hover:file:bg-zinc-200 dark:hover:file:bg-zinc-600
                    cursor-pointer"
                />
                <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
                  Formatos aceitos: JPEG, PNG, WebP. Tamanho máximo: 10MB.
                </p>
              </div>

              {/* Preview */}
              {previewUrl && (
                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
                    Preview
                  </label>
                  <div className="border border-zinc-200 dark:border-zinc-700 rounded-md p-4 bg-zinc-50 dark:bg-zinc-900">
                    <img
                      src={previewUrl}
                      alt="Preview"
                      className="max-w-full h-auto max-h-64 mx-auto rounded"
                    />
                  </div>
                </div>
              )}

              {/* Upload Button */}
              <div className="flex gap-4">
                <button
                  onClick={handleUpload}
                  disabled={!selectedFile || uploading || processing}
                  className="px-4 py-2 bg-black text-white rounded-md hover:bg-zinc-800 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {uploading ? "Enviando..." : processing ? "Processando OCR..." : "Enviar e Processar"}
                </button>
                {previewUrl && (
                  <button
                    onClick={handleReset}
                    disabled={uploading || processing}
                    className="px-4 py-2 border border-zinc-300 dark:border-zinc-600 text-zinc-700 dark:text-zinc-300 rounded-md hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Limpar
                  </button>
                )}
              </div>
            </div>
          </div>
        )}

        {/* OCR Results and Edit Form */}
        {uploadResult && ocrResult && (
          <div className="bg-white dark:bg-zinc-800 rounded-lg shadow-sm border border-zinc-200 dark:border-zinc-700 p-6 mb-6">
            <h2 className="text-xl font-semibold mb-4 dark:text-zinc-100">Resultado do OCR</h2>
            <p className="text-sm text-zinc-600 dark:text-zinc-400 mb-6">
              Revise e edite as informações extraídas antes de salvar o contato.
            </p>

            <div className="grid md:grid-cols-2 gap-6">
              {/* Form Fields */}
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
                    Nome *
                  </label>
                  <input
                    type="text"
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-black dark:focus:ring-zinc-500 focus:border-transparent"
                    placeholder="Primeiro nome"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
                    Sobrenome
                  </label>
                  <input
                    type="text"
                    value={formData.lastName}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                    className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-black dark:focus:ring-zinc-500 focus:border-transparent"
                    placeholder="Sobrenome"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
                    Email
                  </label>
                  <input
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                    className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-black dark:focus:ring-zinc-500 focus:border-transparent"
                    placeholder="email@exemplo.com"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
                    Telefone
                  </label>
                  <input
                    type="tel"
                    value={formData.phone}
                    onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                    className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-black dark:focus:ring-zinc-500 focus:border-transparent"
                    placeholder="(00) 00000-0000"
                  />
                </div>
              </div>

              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
                    Empresa
                  </label>
                  <input
                    type="text"
                    value={formData.company}
                    onChange={(e) => setFormData({ ...formData, company: e.target.value })}
                    className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-black dark:focus:ring-zinc-500 focus:border-transparent"
                    placeholder="Nome da empresa"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
                    Cargo
                  </label>
                  <input
                    type="text"
                    value={formData.jobTitle}
                    onChange={(e) => setFormData({ ...formData, jobTitle: e.target.value })}
                    className="w-full px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:ring-2 focus:ring-black dark:focus:ring-zinc-500 focus:border-transparent"
                    placeholder="Cargo/Função"
                  />
                </div>

                {/* Confidence Scores (if available) */}
                {ocrResult.confidenceScores && (
                  <div>
                    <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
                      Confiança da Extração
                    </label>
                    <div className="space-y-1 text-xs">
                      {Object.entries(ocrResult.confidenceScores).map(([field, score]) => (
                        <div key={field} className="flex justify-between">
                          <span className="text-zinc-600 dark:text-zinc-400 capitalize">{field}:</span>
                          <span className="font-medium text-zinc-900 dark:text-zinc-100">
                            {(score * 100).toFixed(0)}%
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Raw OCR text and AI response side by side */}
            {(ocrResult.rawText !== undefined || ocrResult.aiRawResponse !== undefined) && (
              <div className={`mt-6 grid gap-4 ${ocrResult.rawText !== undefined && ocrResult.aiRawResponse !== undefined ? 'md:grid-cols-2' : 'grid-cols-1'}`}>
                {/* Raw OCR text for verification */}
                {ocrResult.rawText !== undefined && (
                  <div>
                    <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
                      Texto extraA­do (raw)
                    </label>
                    <textarea
                      readOnly
                      value={ocrResult.rawText ?? ""}
                      className="w-full h-56 px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100 text-xs font-mono placeholder:text-zinc-400 dark:placeholder:text-zinc-500"
                      placeholder="Sem texto raw disponA­vel."
                    />
                  </div>
                )}
                
                {/* AI raw response */}
                {ocrResult.aiRawResponse !== undefined && (
                  <div>
                    <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-2">
                      Resposta da IA (raw)
                    </label>
                    <textarea
                      readOnly
                      value={ocrResult.aiRawResponse ?? ""}
                      className="w-full h-56 px-3 py-2 border border-zinc-300 dark:border-zinc-600 rounded-md bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100 text-xs font-mono placeholder:text-zinc-400 dark:placeholder:text-zinc-500"
                      placeholder="Sem resposta da IA disponA­vel."
                    />
                  </div>
                )}
              </div>
            )}

            {/* Action Buttons */}
            <div className="mt-6 flex gap-4">
              <button
                onClick={handleSave}
                disabled={!formData.firstName.trim() || uploading}
                className="px-4 py-2 bg-black text-white rounded-md hover:bg-zinc-800 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {uploading ? "Salvando..." : "Salvar Contato"}
              </button>
              <button
                onClick={handleReset}
                disabled={uploading}
                className="px-4 py-2 border border-zinc-300 dark:border-zinc-600 text-zinc-700 dark:text-zinc-300 rounded-md hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Processar Outro Cartão
              </button>
              {contact && (
                <Link
                  href={`/contatos/${contact.contactId}`}
                  className="px-4 py-2 border border-zinc-300 dark:border-zinc-600 text-zinc-700 dark:text-zinc-300 rounded-md hover:bg-zinc-50 dark:hover:bg-zinc-700 inline-block"
                >
                  Ver Contato
                </Link>
              )}
            </div>
          </div>
        )}

      </div>
    </div>
  );
}
