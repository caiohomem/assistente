'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useTranslations } from 'next-intl'
import { getAgentConfiguration, updateAgentConfiguration, type AgentConfiguration } from '@/lib/api/agentConfigurationApi'
import { LayoutWrapper } from '@/components/LayoutWrapper'
import { Button } from '@/components/ui/button'
import { Bot, Save, X, CheckCircle2, AlertCircle, Clock } from 'lucide-react'

export default function AgentConfigurationPage() {
  const router = useRouter()
  const t = useTranslations('agentConfiguration')
  const tCommon = useTranslations('common')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [configuration, setConfiguration] = useState<AgentConfiguration | null>(null)
  const [ocrPrompt, setOcrPrompt] = useState('')
  const [transcriptionPrompt, setTranscriptionPrompt] = useState('')
  const [workflowPrompt, setWorkflowPrompt] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  useEffect(() => {
    async function loadConfiguration() {
      try {
        setLoading(true)
        setError(null)
        const config = await getAgentConfiguration()
        setConfiguration(config)
        setOcrPrompt(config.ocrPrompt)
        setTranscriptionPrompt(config.transcriptionPrompt || '')
        setWorkflowPrompt(config.workflowPrompt || '')
      } catch (err: any) {
        if (err.message === 'Configuração não encontrada') {
          // Configuração ainda não existe, permitir criar
          setConfiguration(null)
          setOcrPrompt('')
          setWorkflowPrompt('')
        } else {
          console.error('Erro ao carregar configuração:', err)
          setError(err.message || t('errorLoading'))
        }
      } finally {
        setLoading(false)
      }
    }

    loadConfiguration()
  }, [])

  const handleSave = async () => {
    if (!ocrPrompt.trim()) {
      setError(t('promptRequired'))
      return
    }

    try {
      setSaving(true)
      setError(null)
      setSuccess(false)

      const updated = await updateAgentConfiguration({
        ocrPrompt: ocrPrompt.trim(),
        transcriptionPrompt: transcriptionPrompt.trim() || undefined,
        workflowPrompt: workflowPrompt.trim() || undefined
      })

      setConfiguration(updated)
      setSuccess(true)
      
      // Limpar mensagem de sucesso após 3 segundos
      setTimeout(() => setSuccess(false), 3000)
    } catch (err: any) {
      console.error('Erro ao salvar configuração:', err)
      setError(err.message || t('errorSaving'))
    } finally {
      setSaving(false)
    }
  }

  if (loading) {
    return (
      <LayoutWrapper title={t('title')} activeTab="settings">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
            <p className="text-muted-foreground">{t('loading')}</p>
          </div>
        </div>
      </LayoutWrapper>
    )
  }

  return (
    <LayoutWrapper 
      title={t('title')} 
      subtitle={t('description')}
      activeTab="settings"
    >
      <div className="max-w-4xl space-y-6">
        {/* Form */}
        <div className="glass-card p-6">
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
          {error && (
            <div className="mb-6 glass-card border-destructive/50 bg-destructive/10 p-4">
              <div className="flex items-center gap-2">
                <AlertCircle className="w-4 h-4 text-destructive" />
                <p className="text-sm text-destructive">{error}</p>
              </div>
            </div>
          )}

          {/* Success Message */}
          {success && (
            <div className="mb-6 glass-card border-success/50 bg-success/10 p-4">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4 text-success" />
                <p className="text-sm text-success">{t('saveSuccess')}</p>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex gap-4">
            <Button
              onClick={handleSave}
              disabled={saving}
              variant="glow"
            >
              <Save className="w-4 h-4 mr-2" />
              {saving ? t('saving') : t('save')}
            </Button>
            <Button
              onClick={() => router.back()}
              variant="ghost"
            >
              <X className="w-4 h-4 mr-2" />
              {tCommon('cancel')}
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
        </div>
      </div>
    </LayoutWrapper>
  )
}




