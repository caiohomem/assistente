"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { useTranslations } from "next-intl"
import { LayoutWrapper } from "@/components/LayoutWrapper"
import { Button } from "@/components/ui/button"
import { Mail, Bot, Save, CheckCircle2, AlertCircle, Clock, Plus } from "lucide-react"
import Link from "next/link"
import { cn } from "@/lib/utils"
import { ConfirmDialog } from "@/components/ConfirmDialog"
import {
  listEmailTemplatesClient,
  deleteEmailTemplateClient,
  activateEmailTemplateClient,
  deactivateEmailTemplateClient,
  type EmailTemplate,
} from "@/lib/api/emailTemplatesApiClient"
import { EmailTemplateType } from "@/lib/types/emailTemplates"
import { getAgentConfiguration, updateAgentConfiguration, type AgentConfiguration } from "@/lib/api/agentConfigurationApi"

type TabType = "email-templates" | "agent"

export default function ConfiguracoesPage() {
  const router = useRouter()
  const t = useTranslations('agentConfiguration')
  const [activeTab, setActiveTab] = useState<TabType>("email-templates")

  // Email Templates State
  const [loadingTemplates, setLoadingTemplates] = useState(true)
  const [templates, setTemplates] = useState<EmailTemplate[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [totalPages, setTotalPages] = useState(0)
  const [errorTemplates, setErrorTemplates] = useState<string | null>(null)
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; templateId: string | null; templateName: string }>({
    isOpen: false,
    templateId: null,
    templateName: "",
  })
  const [deleting, setDeleting] = useState(false)
  const [filterActiveOnly, setFilterActiveOnly] = useState<boolean | undefined>(undefined)
  const [filterType, setFilterType] = useState<EmailTemplateType | undefined>(undefined)

  // Agent Configuration State
  const [loadingAgent, setLoadingAgent] = useState(true)
  const [saving, setSaving] = useState(false)
  const [configuration, setConfiguration] = useState<AgentConfiguration | null>(null)
  const [ocrPrompt, setOcrPrompt] = useState('')
  const [transcriptionPrompt, setTranscriptionPrompt] = useState('')
  const [workflowPrompt, setWorkflowPrompt] = useState('')
  const [errorAgent, setErrorAgent] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  // Load email templates
  useEffect(() => {
    if (activeTab === "email-templates") {
      loadTemplates()
    }
  }, [page, filterActiveOnly, filterType, activeTab])

  // Load agent configuration
  useEffect(() => {
    if (activeTab === "agent") {
      loadAgentConfiguration()
    }
  }, [activeTab])

  const loadTemplates = async () => {
    setLoadingTemplates(true)
    setErrorTemplates(null)
    try {
      const result = await listEmailTemplatesClient({
        page,
        pageSize,
        activeOnly: filterActiveOnly,
        templateType: filterType,
      })
      setTemplates(result.templates)
      setTotal(result.total)
      setPage(result.page)
      setTotalPages(result.totalPages)
    } catch (err) {
      console.error("Erro ao carregar templates de email:", err)
      setErrorTemplates(err instanceof Error ? err.message : "Erro ao carregar templates de email")
    } finally {
      setLoadingTemplates(false)
    }
  }

  const loadAgentConfiguration = async () => {
    try {
      setLoadingAgent(true)
      setErrorAgent(null)
      const config = await getAgentConfiguration()
      setConfiguration(config)
      setOcrPrompt(config.ocrPrompt)
      setTranscriptionPrompt(config.transcriptionPrompt || '')
      setWorkflowPrompt(config.workflowPrompt || '')
    } catch (err: any) {
      if (err.message === 'ConfiguraÃ§Ã£o nÃ£o encontrada') {
        setConfiguration(null)
        setOcrPrompt('')
        setTranscriptionPrompt('')
        setWorkflowPrompt('')
      } else {
        console.error('Erro ao carregar configuraÃ§Ã£o:', err)
        setErrorAgent(err.message || t('errorLoading'))
      }
    } finally {
      setLoadingAgent(false)
    }
  }

  const getTypeLabel = (type: EmailTemplateType): string => {
    switch (type) {
      case EmailTemplateType.UserCreated:
        return "UsuÃ¡rio Criado"
      case EmailTemplateType.PasswordReset:
        return "RedefiniÃ§Ã£o de Senha"
      case EmailTemplateType.Welcome:
        return "Bem-vindo"
      default:
        return "Desconhecido"
    }
  }

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    })
  }

  const handleDeleteClick = (templateId: string, templateName: string) => {
    setDeleteDialog({
      isOpen: true,
      templateId,
      templateName,
    })
  }

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.templateId) return

    setDeleting(true)
    try {
      await deleteEmailTemplateClient(deleteDialog.templateId)
      setDeleteDialog({ isOpen: false, templateId: null, templateName: "" })
      await loadTemplates()
      router.refresh()
    } catch (err) {
      console.error("Erro ao deletar template:", err)
      setErrorTemplates(err instanceof Error ? err.message : "Erro ao deletar template")
    } finally {
      setDeleting(false)
    }
  }

  const handleDeleteCancel = () => {
    setDeleteDialog({ isOpen: false, templateId: null, templateName: "" })
  }

  const handleToggleActive = async (template: EmailTemplate) => {
    try {
      if (template.isActive) {
        await deactivateEmailTemplateClient(template.id)
      } else {
        await activateEmailTemplateClient(template.id)
      }
      await loadTemplates()
      router.refresh()
    } catch (err) {
      console.error("Erro ao alterar status do template:", err)
      setErrorTemplates(err instanceof Error ? err.message : "Erro ao alterar status do template")
    }
  }

  const handleSaveAgent = async () => {
    if (!ocrPrompt.trim()) {
      setErrorAgent(t('promptRequired'))
      return
    }

    try {
      setSaving(true)
      setErrorAgent(null)
      setSuccess(false)

      const updated = await updateAgentConfiguration({
        ocrPrompt: ocrPrompt.trim(),
        transcriptionPrompt: transcriptionPrompt.trim() || undefined,
        workflowPrompt: workflowPrompt.trim() || undefined
      })

      setConfiguration(updated)
      setSuccess(true)
      
      setTimeout(() => setSuccess(false), 3000)
    } catch (err: any) {
      console.error('Erro ao salvar configuraÃ§Ã£o:', err)
      setErrorAgent(err.message || t('errorSaving'))
    } finally {
      setSaving(false)
    }
  }

  return (
    <LayoutWrapper 
      title="ConfiguraÃ§Ãµes" 
      subtitle="Gerencie templates de email e configuraÃ§Ãµes do agente" 
      activeTab="settings"
    >
      <div className="space-y-6">
        {/* Tabs */}
        <div className="flex gap-2 border-b border-border overflow-x-auto">
          <button
            onClick={() => setActiveTab("email-templates")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "email-templates"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <Mail className="w-4 h-4" />
              Templates de Email
            </div>
          </button>
          <button
            onClick={() => setActiveTab("agent")}
            className={cn(
              "px-4 py-2 text-sm font-medium transition-colors relative whitespace-nowrap",
              activeTab === "agent"
                ? "text-primary border-b-2 border-primary"
                : "text-muted-foreground hover:text-foreground"
            )}
          >
            <div className="flex items-center gap-2">
              <Bot className="w-4 h-4" />
              ConfiguraÃ§Ãµes do Agente
            </div>
          </button>
        </div>

        {/* Content */}
        <div className="glass-card p-6">
          {/* Email Templates Tab */}
          {activeTab === "email-templates" && (
            <div className="space-y-6">
            {loadingTemplates && templates.length === 0 ? (
              <div className="p-12 text-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
                <p className="text-muted-foreground">Carregando templates...</p>
              </div>
            ) : (
              <>
                <div className="flex items-center justify-between mb-6">
                  <div>
                    <h3 className="text-lg font-semibold mb-2">Templates de Email</h3>
                    <p className="text-sm text-muted-foreground">
                      Gerencie os templates de email do sistema, incluindo emails de boas-vindas, recuperaÃ§Ã£o de senha e outros.
                    </p>
                  </div>
                  <Button asChild variant="glow">
                    <Link href="/email-templates/novo">
                      <Plus className="w-4 h-4 mr-2" />
                      Novo Template
                    </Link>
                  </Button>
                </div>

                {errorTemplates && (
                  <div className="border-destructive/50 bg-destructive/10 p-4 rounded-lg mb-4">
                    <p className="text-sm text-destructive">{errorTemplates}</p>
                  </div>
                )}

                {/* Filtros */}
                <div className="flex flex-wrap gap-4 items-center mb-6">
                  <div className="flex items-center gap-2">
                    <label className="text-sm text-muted-foreground">Filtrar:</label>
                    <select
                      value={filterActiveOnly === undefined ? "all" : filterActiveOnly ? "active" : "inactive"}
                      onChange={(e) => {
                        const value = e.target.value
                        setFilterActiveOnly(value === "all" ? undefined : value === "active")
                      }}
                      className="rounded-md border border-border bg-background px-3 py-1 text-sm"
                    >
                      <option value="all">Todos</option>
                      <option value="active">Ativos</option>
                      <option value="inactive">Inativos</option>
                    </select>
                  </div>
                  <div className="flex items-center gap-2">
                    <label className="text-sm text-muted-foreground">Tipo:</label>
                    <select
                      value={filterType === undefined ? "all" : filterType.toString()}
                      onChange={(e) => {
                        const value = e.target.value
                        setFilterType(value === "all" ? undefined : parseInt(value) as EmailTemplateType)
                      }}
                      className="rounded-md border border-border bg-background px-3 py-1 text-sm"
                    >
                      <option value="all">Todos</option>
                      <option value={EmailTemplateType.UserCreated}>UsuÃ¡rio Criado</option>
                      <option value={EmailTemplateType.PasswordReset}>RedefiniÃ§Ã£o de Senha</option>
                      <option value={EmailTemplateType.Welcome}>Bem-vindo</option>
                    </select>
                  </div>
                  <div className="flex-1"></div>
                  <p className="text-sm text-muted-foreground">
                    {total} {total === 1 ? "template encontrado" : "templates encontrados"}
                  </p>
                </div>

                {templates.length === 0 ? (
                  <div className="p-8 text-center">
                    <p className="text-muted-foreground mb-4">Nenhum template encontrado.</p>
                    <Button asChild variant="glow">
                      <Link href="/email-templates/novo">
                        Criar primeiro template
                      </Link>
                    </Button>
                  </div>
                ) : (
                  <>
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                      {templates.map((template) => (
                        <div
                          key={template.id}
                          className="border border-border rounded-lg bg-background/50 p-6 hover:shadow-md transition-shadow"
                        >
                          <Link
                            href={`/email-templates/${template.id}`}
                            className="block"
                          >
                            <div className="flex items-start justify-between mb-2">
                              <h4 className="text-lg font-semibold">
                                {template.name}
                              </h4>
                              {template.isActive ? (
                                <span className="inline-flex items-center rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 px-2.5 py-0.5 text-xs font-medium">
                                  Ativo
                                </span>
                              ) : (
                                <span className="inline-flex items-center rounded-full bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200 px-2.5 py-0.5 text-xs font-medium">
                                  Inativo
                                </span>
                              )}
                            </div>
                            <p className="text-sm text-muted-foreground mb-2">
                              Tipo: {getTypeLabel(template.templateType)}
                            </p>
                            <p className="text-sm mb-2 line-clamp-2">
                              <strong>Assunto:</strong> {template.subject}
                            </p>
                            {template.placeholders.length > 0 && (
                              <p className="text-xs text-muted-foreground mb-2">
                                Placeholders: {template.placeholders.join(", ")}
                              </p>
                            )}
                            <p className="text-xs text-muted-foreground">
                              Criado em: {formatDate(template.createdAt)}
                            </p>
                          </Link>
                          <div className="mt-4 flex gap-2">
                            <Link
                              href={`/email-templates/${template.id}/editar`}
                              className="flex-1 text-center rounded-md border border-border bg-background px-3 py-2 text-sm font-medium hover:bg-secondary"
                            >
                              Editar
                            </Link>
                            <button
                              onClick={(e) => {
                                e.preventDefault()
                                handleToggleActive(template)
                              }}
                              className={`flex-1 rounded-md border px-3 py-2 text-sm font-medium ${
                                template.isActive
                                  ? "border-yellow-300 dark:border-yellow-700 bg-background text-yellow-700 dark:text-yellow-400 hover:bg-yellow-50 dark:hover:bg-yellow-900/20"
                                  : "border-green-300 dark:border-green-700 bg-background text-green-700 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20"
                              }`}
                            >
                              {template.isActive ? "Desativar" : "Ativar"}
                            </button>
                            <button
                              onClick={(e) => {
                                e.preventDefault()
                                handleDeleteClick(template.id, template.name)
                              }}
                              className="flex-1 rounded-md border border-red-300 dark:border-red-700 bg-background px-3 py-2 text-sm font-medium text-red-700 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                            >
                              Excluir
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                    <ConfirmDialog
                      isOpen={deleteDialog.isOpen}
                      title="Excluir Template de Email"
                      message={`Tem certeza que deseja excluir o template "${deleteDialog.templateName}"? Esta aÃ§Ã£o nÃ£o pode ser desfeita.`}
                      confirmText="Excluir"
                      cancelText="Cancelar"
                      onConfirm={handleDeleteConfirm}
                      onCancel={handleDeleteCancel}
                      isLoading={deleting}
                      variant="danger"
                    />

                    {totalPages > 1 && (
                      <div className="flex items-center justify-between">
                        <button
                          onClick={() => setPage(Math.max(1, page - 1))}
                          disabled={page === 1}
                          className="rounded-md border border-border bg-background px-4 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          Anterior
                        </button>
                        <span className="text-sm text-muted-foreground">
                          PÃ¡gina {page} de {totalPages}
                        </span>
                        <button
                          onClick={() => setPage(Math.min(totalPages, page + 1))}
                          disabled={page === totalPages}
                          className="rounded-md border border-border bg-background px-4 py-2 text-sm font-medium hover:bg-secondary disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          PrÃ³xima
                        </button>
                      </div>
                    )}
                  </>
                )}
              </>
            )}
            </div>
          )}

          {/* Agent Configuration Tab */}
          {activeTab === "agent" && (
            <div className="space-y-6">
              {loadingAgent ? (
                <div className="p-12 text-center">
                  <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
                  <p className="text-muted-foreground">{t('loading')}</p>
                </div>
              ) : (
                <>
                  <div className="mb-6">
                    <h3 className="text-lg font-semibold mb-2">ConfiguraÃ§Ãµes do Agente</h3>
                    <p className="text-sm text-muted-foreground">
                      Configure os prompts de OCR e transcriÃ§Ã£o usados pelo agente de IA para processar cartÃµes de visita e notas de Ã¡udio.
                    </p>
                  </div>
                <div className="mb-6">
                  <label htmlFor="ocrPrompt" className="block text-sm font-medium mb-2 flex items-center gap-2">
                    <Bot className="w-4 h-4" />
                    {t('ocrPromptLabel')}
                  </label>
                  <p className="text-sm text-muted-foreground mb-4">
                    {t('ocrPromptHint')}
                  </p>
                  <textarea
                    id="ocrPrompt"
                    value={ocrPrompt}
                    onChange={(e) => setOcrPrompt(e.target.value)}
                    rows={20}
                    className="w-full px-4 py-3 bg-secondary/30 border border-border rounded-xl focus:ring-2 focus:ring-primary/50 focus:border-primary/50 font-mono text-sm transition-all"
                    placeholder={t('ocrPromptPlaceholder')}
                  />
                </div>

                <div className="mb-6">
                  <label htmlFor="transcriptionPrompt" className="block text-sm font-medium mb-2 flex items-center gap-2">
                    <Bot className="w-4 h-4" />
                    {t('transcriptionPromptLabel')}
                  </label>
                  <p className="text-sm text-muted-foreground mb-4">
                    {t('transcriptionPromptHint')}
                  </p>
                  <textarea
                    id="transcriptionPrompt"
                    value={transcriptionPrompt}
                    onChange={(e) => setTranscriptionPrompt(e.target.value)}
                    rows={20}
                    className="w-full px-4 py-3 bg-secondary/30 border border-border rounded-xl focus:ring-2 focus:ring-primary/50 focus:border-primary/50 font-mono text-sm transition-all"
                    placeholder={t('transcriptionPromptPlaceholder')}
                  />
                </div>

                <div className="mb-6">
                  <label htmlFor="workflowPrompt" className="block text-sm font-medium mb-2 flex items-center gap-2">
                    <Bot className="w-4 h-4" />
                    {t('workflowPromptLabel')}
                  </label>
                  <p className="text-sm text-muted-foreground mb-4">
                    {t('workflowPromptHint')}
                  </p>
                  <textarea
                    id="workflowPrompt"
                    value={workflowPrompt}
                    onChange={(e) => setWorkflowPrompt(e.target.value)}
                    rows={12}
                    className="w-full px-4 py-3 bg-secondary/30 border border-border rounded-xl focus:ring-2 focus:ring-primary/50 focus:border-primary/50 font-mono text-sm transition-all"
                    placeholder={t('workflowPromptPlaceholder')}
                  />
                </div>

                  {/* Error Message */}
                  {errorAgent && (
                    <div className="mb-6 border-destructive/50 bg-destructive/10 p-4 rounded-lg">
                      <div className="flex items-center gap-2">
                        <AlertCircle className="w-4 h-4 text-destructive" />
                        <p className="text-sm text-destructive">{errorAgent}</p>
                      </div>
                    </div>
                  )}

                  {/* Success Message */}
                  {success && (
                    <div className="mb-6 border-success/50 bg-success/10 p-4 rounded-lg">
                      <div className="flex items-center gap-2">
                        <CheckCircle2 className="w-4 h-4 text-success" />
                        <p className="text-sm text-success">{t('saveSuccess')}</p>
                      </div>
                    </div>
                  )}

                  {/* Actions */}
                  <div className="flex gap-4">
                    <Button
                      onClick={handleSaveAgent}
                      disabled={saving}
                      variant="glow"
                    >
                      <Save className="w-4 h-4 mr-2" />
                      {saving ? t('saving') : t('save')}
                    </Button>
                  </div>

                  {/* Info */}
                  {configuration && (
                    <div className="mt-6 pt-6 border-t border-border">
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Clock className="w-4 h-4" />
                        <span>
                          {t('lastUpdated')}: {new Date(configuration.updatedAt).toLocaleString('pt-BR')}
                        </span>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>
      </div>
    </LayoutWrapper>
  )
}



