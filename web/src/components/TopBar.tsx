'use client'

import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { ThemeSelector } from './ThemeSelector'
import { UserMenu } from './UserMenu'
import { LanguageSelector } from './LanguageSelector'

interface TopBarProps {
  title?: string
  children?: React.ReactNode
  showBackButton?: boolean
  backHref?: string
}

export function TopBar({ title, children, showBackButton, backHref }: TopBarProps) {
  const router = useRouter()

  const handleBack = () => {
    if (backHref) {
      router.push(backHref)
    } else {
      router.push('/dashboard')
    }
  }

  return (
    <header className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 sticky top-0 z-50 shadow-sm">
      <div className="container mx-auto px-2 sm:px-4 py-3 sm:py-4">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2 sm:gap-4 min-w-0 flex-1">
            {/* Back Button */}
            {showBackButton && (
              <button
                onClick={handleBack}
                className="flex items-center justify-center w-10 h-10 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100"
                aria-label="Voltar"
              >
                <svg
                  className="w-5 h-5"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M15 19l-7-7 7-7"
                  />
                </svg>
              </button>
            )}

            {/* Logo/Title */}
            <Link 
              href="/" 
              className="flex items-center gap-2 text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 transition-colors"
            >
              {/* Icon - always visible */}
              <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-500 flex-shrink-0" />
              {/* Text - hidden on mobile, visible on desktop */}
              <span className="text-2xl font-bold hidden md:inline">
                AssistenteExecutivo
              </span>
            </Link>

            {/* Custom Title */}
            {title && (
              <>
                <div className="h-6 w-px bg-gray-300 dark:bg-gray-600 mx-1 sm:mx-2 hidden sm:block" />
                <h1 className="text-lg sm:text-xl font-semibold text-gray-900 dark:text-gray-100 truncate">{title}</h1>
              </>
            )}

            {/* Custom Content */}
            {children}
          </div>

          {/* User Info, Language Selector, Theme Selector */}
          <div className="flex items-center gap-2 sm:gap-4 flex-shrink-0">
            <LanguageSelector />
            <ThemeSelector />
            <UserMenu />
          </div>
        </div>
      </div>
    </header>
  )
}

