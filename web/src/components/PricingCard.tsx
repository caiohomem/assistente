'use client'

import { ReactNode } from 'react'

export interface PricingCardProps {
  name: string
  price: string
  features: string[]
  highlighted?: boolean
  popularLabel?: string
  selectPlanLabel?: string
  onSelect?: () => void
  children?: ReactNode
}

export function PricingCard({
  name,
  price,
  features,
  highlighted = false,
  popularLabel = 'Popular',
  selectPlanLabel = 'Selecionar Plano',
  onSelect,
  children,
}: PricingCardProps) {
  return (
    <div
      className={`relative flex flex-col rounded-2xl border-2 transition-all duration-300 ${
        highlighted
          ? 'border-indigo-500 bg-gradient-to-br from-indigo-50 to-purple-50 dark:from-indigo-900/20 dark:to-purple-900/20 shadow-xl scale-105'
          : 'border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 hover:border-indigo-300 dark:hover:border-indigo-600 hover:shadow-lg'
      }`}
    >
      {highlighted && (
        <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
          <span className="bg-gradient-to-r from-indigo-500 to-purple-500 text-white text-xs font-semibold px-4 py-1 rounded-full">
            {popularLabel}
          </span>
        </div>
      )}

      <div className="p-8 flex flex-col flex-1">
        <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">{name}</h3>
        
        <div className="mb-6">
          <span className="text-4xl font-extrabold text-gray-900 dark:text-white">{price}</span>
        </div>

        <ul className="flex-1 space-y-4 mb-8">
          {features.map((feature, index) => (
            <li key={index} className="flex items-start gap-3">
              <svg
                className={`w-5 h-5 mt-0.5 flex-shrink-0 ${
                  highlighted
                    ? 'text-indigo-500'
                    : 'text-gray-400 dark:text-gray-500'
                }`}
                fill="currentColor"
                viewBox="0 0 20 20"
              >
                <path
                  fillRule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
                  clipRule="evenodd"
                />
              </svg>
              <span className="text-gray-700 dark:text-gray-300">{feature}</span>
            </li>
          ))}
        </ul>

        {children && <div className="mb-6">{children}</div>}

        {onSelect && (
          <button
            onClick={onSelect}
            className={`w-full py-3 px-6 rounded-lg font-semibold transition-all duration-300 ${
              highlighted
                ? 'bg-gradient-to-r from-indigo-500 to-purple-500 text-white hover:from-indigo-600 hover:to-purple-600 shadow-lg hover:shadow-xl transform hover:scale-105'
                : 'bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-white hover:bg-gray-200 dark:hover:bg-gray-600'
            }`}
          >
            {selectPlanLabel}
          </button>
        )}
      </div>
    </div>
  )
}

