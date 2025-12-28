'use client'

import { useTheme } from '@/lib/theme'

export function ThemeSelector() {
  const { theme, resolvedTheme, mounted, setTheme } = useTheme()

  const toggleTheme = () => {
    let newTheme: 'light' | 'dark' | 'system'
    if (theme === 'light') {
      newTheme = 'dark'
    } else if (theme === 'dark') {
      newTheme = 'system'
    } else {
      newTheme = 'light'
    }
    
    setTheme(newTheme)
  }

  const getThemeIcon = () => {
    // Durante SSR ou antes do mount, retornar ícone do sistema para evitar hydration mismatch
    if (!mounted || theme === 'system') {
      return (
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
            d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"
          />
        </svg>
      )
    }
    if (resolvedTheme === 'dark') {
      return (
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
            d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
          />
        </svg>
      )
    }
    return (
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
          d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
        />
      </svg>
    )
  }

  const getThemeLabel = () => {
    // Durante SSR ou antes do mount, retornar um valor padrão para evitar hydration mismatch
    if (!mounted) {
      return 'Sistema'
    }
    if (theme === 'system') {
      return 'Sistema'
    }
    return resolvedTheme === 'dark' ? 'Escuro' : 'Claro'
  }

  return (
    <div className="relative">
      <button
        onClick={toggleTheme}
        className="flex items-center gap-2 px-3 py-2 bg-secondary/50 backdrop-blur-sm border border-border/50 rounded-2xl hover:bg-secondary/80 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50 transition-all duration-300 hover:scale-105"
        aria-label={`Alternar tema. Tema atual: ${getThemeLabel()}`}
        title={`Tema: ${getThemeLabel()} (clique para alternar)`}
        suppressHydrationWarning
      >
        <div className="w-4 h-4 lg:w-5 lg:h-5 text-foreground">
          {getThemeIcon()}
        </div>
        <span 
          className="text-sm font-medium text-foreground hidden sm:inline"
          suppressHydrationWarning
        >
          {getThemeLabel()}
        </span>
      </button>
    </div>
  )
}
