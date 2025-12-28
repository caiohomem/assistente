'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { getCreditBalance, listCreditTransactions, purchaseCreditPackage } from '@/lib/api/creditsApi'
import { getBffSession } from '@/lib/bff'
import type { CreditBalance, CreditTransaction } from '@/lib/types/credit'
import { CreditTransactionType } from '@/lib/types/credit'
import type { CreditPackage } from '@/lib/types/plan'
import { LayoutWrapper } from '@/components/LayoutWrapper'
import { SelectCreditPackageModal } from '@/components/SelectCreditPackageModal'
import { Button } from '@/components/ui/button'
import { Coins, Plus, TrendingUp, TrendingDown } from 'lucide-react'
import { cn } from '@/lib/utils'

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
      <LayoutWrapper title="Créditos" subtitle="Gerencie seu saldo de créditos" activeTab="credits">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
            <p className="text-muted-foreground">Carregando créditos...</p>
          </div>
        </div>
      </LayoutWrapper>
    )
  }

  if (error) {
    return (
      <LayoutWrapper title="Créditos" subtitle="Gerencie seu saldo de créditos" activeTab="credits">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <p className="text-destructive mb-4">{error}</p>
            <button
              onClick={() => window.location.reload()}
              className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
            >
              Tentar novamente
            </button>
          </div>
        </div>
      </LayoutWrapper>
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
    <LayoutWrapper title="Créditos" subtitle="Gerencie seu saldo de créditos" activeTab="credits">
      <div className="space-y-6">
        {/* Balance Card - Hero Section */}
        <div className="glass-card p-8 relative overflow-hidden animate-slide-up">
          {/* Background gradient effect */}
          <div className="absolute inset-0 bg-gradient-to-br from-primary/10 via-primary/5 to-transparent pointer-events-none" />
          
          <div className="relative z-10">
            <div className="flex items-start justify-between mb-6">
              <div className="flex-1">
                <div className="flex items-center gap-3 mb-2">
                  <div className="w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center">
                    <Coins className="w-6 h-6 text-primary" />
                  </div>
                  <div>
                    <h2 className="text-lg font-medium text-muted-foreground">Saldo Disponível</h2>
                    <div className="text-4xl lg:text-5xl font-bold mt-1">
                      {balance ? formatCurrency(balance.balance) : formatCurrency(0)}
                    </div>
                  </div>
                </div>
                <p className="text-sm text-muted-foreground mt-2">
                  {balance?.transactionCount || 0} transação{balance?.transactionCount !== 1 ? 'ões' : ''} realizada{balance?.transactionCount !== 1 ? 's' : ''}
                </p>
              </div>
            </div>
            
            <Button
              variant="glow"
              size="lg"
              onClick={() => setShowPackageModal(true)}
              className="w-full sm:w-auto"
            >
              <Plus className="w-4 h-4 mr-2" />
              Adicionar Saldo
            </Button>
          </div>
        </div>

        {/* Transactions Section */}
        <div className="glass-card p-6 animate-slide-up" style={{ animationDelay: '100ms' }}>
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-xl font-semibold">
              Histórico de Transações
            </h3>
          </div>
          
          {transactions.length === 0 ? (
            <div className="text-center py-16">
              <div className="w-16 h-16 rounded-full bg-secondary/50 flex items-center justify-center mx-auto mb-4">
                <Coins className="w-8 h-8 text-muted-foreground" />
              </div>
              <p className="text-muted-foreground font-medium">
                Nenhuma transação encontrada
              </p>
              <p className="text-sm text-muted-foreground mt-1">
                Suas transações de créditos aparecerão aqui
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              {transactions.map((transaction, index) => {
                const isPositive = transaction.type === CreditTransactionType.Grant || 
                                  transaction.type === CreditTransactionType.Purchase || 
                                  transaction.type === CreditTransactionType.Refund
                
                return (
                  <div
                    key={transaction.transactionId}
                    className={cn(
                      "flex items-center gap-4 p-4 rounded-xl transition-all duration-200",
                      "bg-secondary/30 hover:bg-secondary/50 border border-border/50",
                      "animate-slide-up"
                    )}
                    style={{ animationDelay: `${(index + 1) * 50}ms` }}
                  >
                    {/* Icon */}
                    <div className={cn(
                      "w-10 h-10 rounded-lg flex items-center justify-center flex-shrink-0",
                      isPositive 
                        ? "bg-green-500/10 text-green-600 dark:text-green-400" 
                        : "bg-red-500/10 text-red-600 dark:text-red-400"
                    )}>
                      {isPositive ? (
                        <TrendingUp className="w-5 h-5" />
                      ) : (
                        <TrendingDown className="w-5 h-5" />
                      )}
                    </div>
                    
                    {/* Content */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between gap-4 mb-1">
                        <span className={cn(
                          "font-semibold text-sm",
                          isPositive 
                            ? "text-green-600 dark:text-green-400" 
                            : "text-red-600 dark:text-red-400"
                        )}>
                          {getTransactionTypeLabel(transaction.type)}
                        </span>
                        <span className={cn(
                          "text-lg font-bold whitespace-nowrap",
                          isPositive 
                            ? "text-green-600 dark:text-green-400" 
                            : "text-red-600 dark:text-red-400"
                        )}>
                          {isPositive ? '+' : '-'}{formatCurrency(Math.abs(transaction.amount))}
                        </span>
                      </div>
                      
                      {transaction.reason && (
                        <p className="text-sm text-muted-foreground mb-1 line-clamp-1">
                          {transaction.reason}
                        </p>
                      )}
                      
                      <p className="text-xs text-muted-foreground">
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
                )
              })}
            </div>
          )}
        </div>

        {/* Package Selection Modal */}
        <SelectCreditPackageModal
          isOpen={showPackageModal}
          onClose={() => setShowPackageModal(false)}
          onSelectPackage={handleSelectPackage}
          purchasing={purchasing}
        />
      </div>
    </LayoutWrapper>
  )
}


