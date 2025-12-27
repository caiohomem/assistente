'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useLocale } from 'next-intl'
import { useTranslations } from 'next-intl'
import Link from 'next/link'
import { getCreditBalance, listCreditTransactions } from '@/lib/api/creditsApi'
import { listContactsClient } from '@/lib/api/contactsApiClient'
import { getBffSession } from '@/lib/bff'
import type { CreditBalance, CreditTransaction } from '@/lib/types/credit'
import { CreditTransactionType } from '@/lib/types/credit'
import { TopBar } from '@/components/TopBar'

interface DashboardStats {
  totalContacts: number
  creditBalance: CreditBalance | null
  recentTransactions: CreditTransaction[]
  firstContactId: string | null
}

export default function DashboardPage() {
  const router = useRouter()
  const locale = useLocale()
  const t = useTranslations('dashboard')
  const tCommon = useTranslations('common')
  const [loading, setLoading] = useState(true)
  const [stats, setStats] = useState<DashboardStats>({
    totalContacts: 0,
    creditBalance: null,
    recentTransactions: [],
    firstContactId: null
  })
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true;
    
    async function loadDashboardData() {
      try {
        // Verificar autenticação
        console.log('[Dashboard] Verificando autenticação...');
        const session = await getBffSession();
        console.log('[Dashboard] Sessão recebida:', { 
          authenticated: session.authenticated, 
          hasUser: !!session.user,
          userEmail: session.user?.email 
        });
        
        if (!isMounted) return;
        
        if (!session.authenticated) {
          console.log('[Dashboard] Não autenticado, redirecionando para login');
          
          // Verificar se já estamos em um loop de redirect
          const urlParams = new URLSearchParams(window.location.search);
          const redirectCount = parseInt(sessionStorage.getItem('redirectCount') || '0');
          
          if (redirectCount > 3) {
            console.error('[Dashboard] Loop de redirect detectado! Parando para evitar loop infinito.');
            setError('Erro de autenticação. Por favor, limpe os cookies e tente novamente.');
            sessionStorage.removeItem('redirectCount');
            return;
          }
          
          sessionStorage.setItem('redirectCount', (redirectCount + 1).toString());
          
          // Usar window.location.href ao invés de router.push para evitar loops
          const currentPath = window.location.pathname;
          window.location.href = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
          return;
        }
        
        // Se autenticado, limpar contador de redirects
        sessionStorage.removeItem('redirectCount');
        
        console.log('[Dashboard] Autenticado, carregando dados...');

        // Carregar dados em paralelo
        const [balanceResult, transactionsResult, contactsResult] = await Promise.allSettled([
          getCreditBalance(),
          listCreditTransactions(),
          listContactsClient({ page: 1, pageSize: 1 }) // Para obter o total e o primeiro contato
        ])

        const newStats: DashboardStats = {
          totalContacts: 0,
          creditBalance: null,
          recentTransactions: [],
          firstContactId: null
        }

        // Processar saldo
        if (balanceResult.status === 'fulfilled') {
          newStats.creditBalance = balanceResult.value
        }

        // Processar transações (últimas 5)
        if (transactionsResult.status === 'fulfilled') {
          newStats.recentTransactions = transactionsResult.value
            .sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime())
            .slice(0, 5)
        }

        // Processar total de contatos e primeiro contato
        if (contactsResult.status === 'fulfilled') {
          newStats.totalContacts = contactsResult.value.total
          // Se houver contatos, pegar o ID do primeiro
          if (contactsResult.value.contacts.length > 0) {
            newStats.firstContactId = contactsResult.value.contacts[0].contactId
          }
        }

        setStats(newStats)
      } catch (err) {
        console.error('Erro ao carregar dashboard:', err)
        setError(t('error'))
      } finally {
        setLoading(false)
      }
    }

    loadDashboardData();
    
    return () => {
      isMounted = false;
    };
  }, [router])

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

  if (error) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 flex items-center justify-center">
        <div className="text-center">
          <p className="text-red-600 dark:text-red-400 mb-4">{error}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700"
          >
            {tCommon('tryAgain')}
          </button>
        </div>
      </div>
    )
  }

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: locale === 'pt-BR' || locale === 'pt-PT' ? 'BRL' : 
                locale === 'en-US' ? 'USD' :
                locale === 'es-ES' ? 'EUR' :
                locale === 'fr-FR' ? 'EUR' :
                locale === 'it-IT' ? 'EUR' : 'BRL'
    }).format(value)
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
      <TopBar title={t('title')} showBackButton={false} />

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-7xl mx-auto">
          {/* Welcome Section */}
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-8 mb-8">
            <h2 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
              {t('welcome')}
            </h2>
            <p className="text-gray-600 dark:text-gray-300">
              {t('welcomeDescription')}
            </p>
          </div>

          {/* Credit Balance Card */}
          <div className="bg-gradient-to-br from-indigo-600 to-indigo-700 dark:from-indigo-700 dark:to-indigo-800 rounded-2xl shadow-xl p-6 text-white mb-8">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold">{t('creditBalance')}</h3>
              <div className="text-3xl">💰</div>
            </div>
            <div className="text-4xl font-bold mb-2">
              {stats.creditBalance ? formatCurrency(stats.creditBalance.balance) : formatCurrency(0)}
            </div>
            <div className="text-indigo-100 text-sm">
              {stats.creditBalance?.transactionCount || 0} {t('transactionsPerformed')}
            </div>
          </div>

          {/* Functionality Cards Grid */}
          <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
            {/* Contatos */}
            <Link
              href="/contatos"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">👥</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  {t('contacts')}
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                {t('contactsDescription')}
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
              {stats.totalContacts > 0 && (
                <div className="mt-4 text-sm text-gray-500 dark:text-gray-400">
                  {stats.totalContacts} {stats.totalContacts !== 1 ? t('contactsPlural') : t('contact')} {stats.totalContacts !== 1 ? t('registeredPlural') : t('registered')}
                </div>
              )}
            </Link>

            {/* Créditos */}
            <Link
              href="/creditos"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">💳</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  {t('credits')}
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                {t('creditsDescription')}
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
              {stats.creditBalance && (
                <div className="mt-4 text-sm text-gray-500 dark:text-gray-400">
                  {t('balance')}: {formatCurrency(stats.creditBalance.balance)}
                </div>
              )}
            </Link>

            {/* Upload de Mídia */}
            <Link
              href="/contatos/upload-cartao"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">📸</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  {t('uploadMedia')}
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                {t('uploadMediaDescription')}
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </Link>

            {/* Configurações do Agente */}
            <Link
              href="/configuracoes-agente"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">🤖</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  {t('agentSettings')}
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                {t('agentSettingsDescription')}
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </Link>

            {/* Lembretes */}
            <Link
              href="/automacao/lembretes"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">⏰</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  Lembretes
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                Gerencie seus lembretes e agendamentos
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </Link>

            {/* Rascunhos */}
            <Link
              href="/automacao/rascunhos"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">📝</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  Rascunhos
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                Crie e gerencie rascunhos de documentos
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </Link>

            {/* Templates */}
            <Link
              href="/automacao/templates"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">📄</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  Templates
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                Gerencie seus templates reutilizáveis
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </Link>

            {/* Papéis Timbrados */}
            <Link
              href="/automacao/papeis-timbrados"
              className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6 hover:shadow-xl transition-shadow group"
            >
              <div className="flex items-center mb-4">
                <div className="text-4xl mr-4">✉️</div>
                <h3 className="text-xl font-bold text-gray-900 dark:text-gray-100 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                  Papéis Timbrados
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                Gerencie seus papéis timbrados personalizados
              </p>
              <div className="flex items-center text-indigo-600 dark:text-indigo-400 font-semibold">
                {tCommon('access')}
                <svg className="w-5 h-5 ml-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </Link>
          </div>

          {/* Recent Activities */}
          {stats.recentTransactions.length > 0 && (
            <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6">
              <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
                {t('recentActivities')}
              </h3>
              <div className="space-y-3">
                {stats.recentTransactions.slice(0, 5).map((transaction) => {
                  const isPositive = transaction.type === CreditTransactionType.Grant || 
                                    transaction.type === CreditTransactionType.Purchase || 
                                    transaction.type === CreditTransactionType.Refund
                  
                  return (
                    <div
                      key={transaction.transactionId}
                      className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-700 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors"
                    >
                      <div className="flex items-center gap-4 flex-1">
                        <div className={`text-2xl ${isPositive ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                          {isPositive ? '↑' : '↓'}
                        </div>
                        <div className="flex-1">
                          <div className="flex items-center gap-2 mb-1">
                            <span className={`font-semibold ${isPositive ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'}`}>
                              {transaction.type === CreditTransactionType.Grant ? t('transactionTypes.grant') :
                               transaction.type === CreditTransactionType.Purchase ? t('transactionTypes.purchase') :
                               transaction.type === CreditTransactionType.Consume ? t('transactionTypes.consume') :
                               transaction.type === CreditTransactionType.Refund ? t('transactionTypes.refund') :
                               transaction.type === CreditTransactionType.Expire ? t('transactionTypes.expire') : t('transactionTypes.reserve')}
                            </span>
                            <span className="text-sm text-gray-600 dark:text-gray-400">
                              {formatCurrency(transaction.amount)}
                            </span>
                          </div>
                          {transaction.reason && (
                            <p className="text-sm text-gray-600 dark:text-gray-400">
                              {transaction.reason}
                            </p>
                          )}
                          <p className="text-xs text-gray-500 dark:text-gray-500 mt-1">
                            {new Date(transaction.occurredAt).toLocaleDateString(locale, {
                              day: '2-digit',
                              month: '2-digit',
                              year: 'numeric',
                              hour: '2-digit',
                              minute: '2-digit'
                            })}
                          </p>
                        </div>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  )
}

