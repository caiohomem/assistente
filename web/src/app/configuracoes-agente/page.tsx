'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useTranslations } from 'next-intl'
import { getAgentConfiguration, updateAgentConfiguration, type AgentConfiguration } from '@/lib/api/agentConfigurationApi'
import { TopBar } from '@/components/TopBar'

export default function AgentConfigurationPage() {
  const router = useRouter()
  const t = useTranslations('agentConfiguration')
  const tCommon = useTranslations('common')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [configuration, setConfiguration] = useState<AgentConfiguration | null>(null)
  const [ocrPrompt, setOcrPrompt] = useState('')
  const [transcriptionPrompt, setTranscriptionPrompt] = useState('')
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
      } catch (err: any) {
        if (err.message === 'Configuração não encontrada') {
          // Configuração ainda não existe, permitir criar
          setConfiguration(null)
          setOcrPrompt('')
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
        transcriptionPrompt: transcriptionPrompt.trim() || undefined
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
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-300">{t('loading')}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
      <TopBar title={t('title')} showBackButton={true} />

      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          {/* Header */}
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-8 mb-8">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
              {t('title')}
            </h2>
            <p className="text-gray-600 dark:text-gray-300">
              {t('description')}
            </p>
          </div>

          {/* Form */}
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-8">
            <div className="mb-6">
              <label htmlFor="ocrPrompt" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                {t('ocrPromptLabel')}
              </label>
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                {t('ocrPromptHint')}
              </p>
              <textarea
                id="ocrPrompt"
                value={ocrPrompt}
                onChange={(e) => setOcrPrompt(e.target.value)}
                rows={20}
                className="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 dark:bg-gray-700 dark:text-gray-100 font-mono text-sm"
                placeholder={t('ocrPromptPlaceholder')}
              />
            </div>

            <div className="mb-6">
              <label htmlFor="transcriptionPrompt" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                {t('transcriptionPromptLabel')}
              </label>
              <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
                {t('transcriptionPromptHint')}
              </p>
              <textarea
                id="transcriptionPrompt"
                value={transcriptionPrompt}
                onChange={(e) => setTranscriptionPrompt(e.target.value)}
                rows={20}
                className="w-full px-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 dark:bg-gray-700 dark:text-gray-100 font-mono text-sm"
                placeholder={t('transcriptionPromptPlaceholder')}
              />
            </div>

            {/* Error Message */}
            {error && (
              <div className="mb-6 p-4 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
                <p className="text-sm text-red-800 dark:text-red-200">{error}</p>
              </div>
            )}

            {/* Success Message */}
            {success && (
              <div className="mb-6 p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg">
                <p className="text-sm text-green-800 dark:text-green-200">{t('saveSuccess')}</p>
              </div>
            )}

            {/* Actions */}
            <div className="flex gap-4">
              <button
                onClick={handleSave}
                disabled={saving}
                className="px-6 py-3 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed font-semibold transition-colors"
              >
                {saving ? t('saving') : t('save')}
              </button>
              <button
                onClick={() => router.back()}
                className="px-6 py-3 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 font-semibold transition-colors"
              >
                {tCommon('cancel')}
              </button>
            </div>

            {/* Info */}
            {configuration && (
              <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {t('lastUpdated')}: {new Date(configuration.updatedAt).toLocaleString()}
                </p>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}





