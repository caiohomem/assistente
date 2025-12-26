'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { getCreditBalance, listCreditTransactions, purchaseCreditPackage } from '@/lib/api/creditsApi'
import { getBffSession } from '@/lib/bff'
import type { CreditBalance, CreditTransaction } from '@/lib/types/credit'
import { CreditTransactionType } from '@/lib/types/credit'
import type { CreditPackage } from '@/lib/types/plan'
import { TopBar } from '@/components/TopBar'
import { SelectCreditPackageModal } from '@/components/SelectCreditPackageModal'

export default function CreditosPage() {
  const router = useRouter()
  const [loading, setLoading] = useState(true)
  const [balance, setBalance] = useState<CreditBalance | null>(null)
  const [transactions, setTransactions] = useState<CreditTransaction[]>([])
  const [error, setError] = useState<string | null>(null)
  const [showPackageModal, setShowPackageModal] = useState(false)
  const [purchasing, setPurchasing] = useState(false)

  useEffect(() => {
    loadCreditsData()
  }, [router])

  if (loading) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-300">Carregando créditos...</p>
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
            Tentar novamente
          </button>
        </div>
      </div>
    )
  }

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value)
  }

  const getTransactionTypeLabel = (type: CreditTransactionType) => {
    switch (type) {
      case CreditTransactionType.Grant:
        return 'Concedido'
      case CreditTransactionType.Purchase:
        return 'Comprado'
      case CreditTransactionType.Consume:
        return 'Consumido'
      case CreditTransactionType.Refund:
        return 'Reembolsado'
      case CreditTransactionType.Expire:
        return 'Expirado'
      case CreditTransactionType.Reserve:
        return 'Reservado'
      default:
        return 'Desconhecido'
    }
  }

  const handleSelectPackage = async (pkg: CreditPackage) => {
    try {
      setPurchasing(true)
      
      // Chamar API para comprar o pacote
      const result = await purchaseCreditPackage({ packageId: pkg.packageId })
      
      // Fechar modal
      setShowPackageModal(false)
      
      // Recarregar dados de créditos
      await loadCreditsData()
      
      // Mostrar mensagem de sucesso (opcional)
      // Você pode adicionar um toast/notificação aqui
      console.log(`Pacote ${result.packageName} comprado com sucesso! ${result.creditsAdded} créditos adicionados.`)
    } catch (err: any) {
      console.error('Erro ao comprar pacote:', err)
      setError(err?.message || 'Erro ao comprar pacote de créditos')
    } finally {
      setPurchasing(false)
    }
  }

  async function loadCreditsData() {
    try {
      setLoading(true)
      setError(null)
      
      // Verificar autenticação
      const session = await getBffSession()
      if (!session.authenticated) {
        router.push('/login')
        return
      }

      // Carregar dados em paralelo
      const [balanceResult, transactionsResult] = await Promise.allSettled([
        getCreditBalance(),
        listCreditTransactions()
      ])

      // Processar saldo
      if (balanceResult.status === 'fulfilled') {
        setBalance(balanceResult.value)
      } else {
        console.error('Erro ao carregar saldo:', balanceResult.reason)
      }

      // Processar transações
      if (transactionsResult.status === 'fulfilled') {
        const sortedTransactions = transactionsResult.value
          .sort((a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime())
        setTransactions(sortedTransactions)
      } else {
        console.error('Erro ao carregar transações:', transactionsResult.reason)
      }
    } catch (err) {
      console.error('Erro ao carregar dados de créditos:', err)
      setError('Erro ao carregar dados de créditos')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-indigo-50 to-purple-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900">
      <TopBar title="Créditos" showBackButton={true} />

      {/* Main Content */}
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          {/* Balance Card */}
          <div className="bg-gradient-to-br from-indigo-600 to-indigo-700 dark:from-indigo-700 dark:to-indigo-800 rounded-2xl shadow-xl p-8 text-white mb-8">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-2xl font-bold">Saldo de Créditos</h2>
              <div className="text-5xl">💰</div>
            </div>
            <div className="text-5xl font-bold mb-4">
              {balance ? formatCurrency(balance.balance) : formatCurrency(0)}
            </div>
            <div className="text-indigo-100 text-sm mb-6">
              {balance?.transactionCount || 0} transação{balance?.transactionCount !== 1 ? 'ões' : ''} realizada{balance?.transactionCount !== 1 ? 's' : ''}
            </div>
            <button
              onClick={() => setShowPackageModal(true)}
              className="w-full sm:w-auto px-6 py-3 bg-white text-indigo-600 font-semibold rounded-lg hover:bg-indigo-50 transition-colors shadow-lg hover:shadow-xl transform hover:scale-105"
            >
              + Adicionar Saldo
            </button>
          </div>

          {/* Transactions Section */}
          <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-6">
            <h3 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-6">
              Histórico de Transações
            </h3>
            
            {transactions.length === 0 ? (
              <div className="text-center py-12">
                <div className="text-4xl mb-4">📋</div>
                <p className="text-gray-600 dark:text-gray-400">
                  Nenhuma transação encontrada
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                {transactions.map((transaction) => {
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
                              {getTransactionTypeLabel(transaction.type)}
                            </span>
                            <span className="text-lg font-bold text-gray-900 dark:text-gray-100">
                              {formatCurrency(transaction.amount)}
                            </span>
                          </div>
                          {transaction.reason && (
                            <p className="text-sm text-gray-600 dark:text-gray-400 mb-1">
                              {transaction.reason}
                            </p>
                          )}
                          <p className="text-xs text-gray-500 dark:text-gray-500">
                            {new Date(transaction.occurredAt).toLocaleDateString('pt-BR', {
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
            )}
          </div>
        </div>
      </main>

      {/* Package Selection Modal */}
      <SelectCreditPackageModal
        isOpen={showPackageModal}
        onClose={() => setShowPackageModal(false)}
        onSelectPackage={handleSelectPackage}
        purchasing={purchasing}
      />
    </div>
  )
}


