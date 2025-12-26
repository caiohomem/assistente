'use client'

import { useEffect, useState } from 'react'
import { listCreditPackages } from '@/lib/api/creditsApi'
import type { CreditPackage } from '@/lib/types/plan'

interface SelectCreditPackageModalProps {
  isOpen: boolean
  onClose: () => void
  onSelectPackage: (pkg: CreditPackage) => void | Promise<void>
  purchasing?: boolean
}

export function SelectCreditPackageModal({
  isOpen,
  onClose,
  onSelectPackage,
  purchasing = false,
}: SelectCreditPackageModalProps) {
  const [packages, setPackages] = useState<CreditPackage[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (isOpen) {
      loadPackages()
    }
  }, [isOpen])

  async function loadPackages() {
    try {
      setLoading(true)
      setError(null)
      const pkgs = await listCreditPackages()
      setPackages(pkgs.filter(pkg => pkg.isActive))
    } catch (err) {
      console.error('Erro ao carregar pacotes:', err)
      setError('Erro ao carregar pacotes de crédito')
    } finally {
      setLoading(false)
    }
  }

  const formatCurrency = (value: number, currency: string = 'BRL') => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: currency
    }).format(value)
  }

  const formatAmount = (amount: number) => {
    if (amount === -1) {
      return 'Ilimitado'
    }
    return new Intl.NumberFormat('pt-BR').format(amount)
  }

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50">
      <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl max-w-4xl w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="sticky top-0 bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-6 py-4 flex items-center justify-between rounded-t-2xl">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
            Adicionar Saldo
          </h2>
          <button
            onClick={onClose}
            className="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 transition-colors"
            aria-label="Fechar"
          >
            <svg
              className="w-6 h-6"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="p-6">
          {loading ? (
            <div className="text-center py-12">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
              <p className="text-gray-600 dark:text-gray-300">Carregando pacotes...</p>
            </div>
          ) : error ? (
            <div className="text-center py-12">
              <div className="text-4xl mb-4">⚠️</div>
              <p className="text-red-600 dark:text-red-400 mb-4">{error}</p>
              <button
                onClick={loadPackages}
                className="px-4 py-2 bg-indigo-600 text-white rounded-md hover:bg-indigo-700"
              >
                Tentar novamente
              </button>
            </div>
          ) : packages.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-4xl mb-4">📦</div>
              <p className="text-gray-600 dark:text-gray-400">
                Nenhum pacote disponível no momento
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {packages.map((pkg) => (
                <div
                  key={pkg.packageId}
                  className="relative flex flex-col rounded-2xl border-2 border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-indigo-300 dark:hover:border-indigo-600 hover:shadow-lg transition-all duration-300"
                >
                  <div className="p-6 flex flex-col flex-1">
                    <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-2">
                      {pkg.name}
                    </h3>
                    
                    <div className="mb-4">
                      <div className="text-3xl font-extrabold text-gray-900 dark:text-white">
                        {formatCurrency(pkg.price, pkg.currency)}
                      </div>
                      {pkg.description && (
                        <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                          {pkg.description}
                        </p>
                      )}
                    </div>

                    <div className="mb-6 flex-1">
                      <div className="flex items-center gap-2">
                        <svg
                          className="w-5 h-5 text-indigo-500"
                          fill="currentColor"
                          viewBox="0 0 20 20"
                        >
                          <path
                            fillRule="evenodd"
                            d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                            clipRule="evenodd"
                          />
                        </svg>
                        <span className="text-lg font-semibold text-gray-900 dark:text-white">
                          {formatAmount(pkg.amount)} créditos
                        </span>
                      </div>
                    </div>

                    <button
                      onClick={() => onSelectPackage(pkg)}
                      disabled={purchasing}
                      className="w-full py-3 px-6 rounded-lg font-semibold bg-gradient-to-r from-indigo-500 to-purple-500 text-white hover:from-indigo-600 hover:to-purple-600 shadow-lg hover:shadow-xl transform hover:scale-105 transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none"
                    >
                      {purchasing ? 'Processando...' : 'Selecionar'}
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-gray-50 dark:bg-gray-900 border-t border-gray-200 dark:border-gray-700 px-6 py-4 flex justify-end rounded-b-2xl">
          <button
            onClick={onClose}
            className="px-4 py-2 text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white transition-colors"
          >
            Cancelar
          </button>
        </div>
      </div>
    </div>
  )
}

