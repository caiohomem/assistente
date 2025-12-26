'use client'

import { useTheme } from '@/lib/theme'

export function ThemeSelector() {
  const { theme, resolvedTheme, mounted, setTheme } = useTheme()

  const toggleTheme = () => {
    // #region agent log
    fetch('http://127.0.0.1:7244/ingest/c003d7a1-2df5-4d85-8124-323cc6c30d9d',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ThemeSelector.tsx:8',message:'toggleTheme called',data:{currentTheme:theme,resolvedTheme,mounted},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
    // #endregion
    let newTheme: 'light' | 'dark' | 'system'
    if (theme === 'light') {
      newTheme = 'dark'
    } else if (theme === 'dark') {
      newTheme = 'system'
    } else {
      newTheme = 'light'
    }
    
    // #region agent log
    fetch('http://127.0.0.1:7244/ingest/c003d7a1-2df5-4d85-8124-323cc6c30d9d',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({location:'ThemeSelector.tsx:18',message:'About to call setTheme',data:{newTheme,currentTheme:theme},timestamp:Date.now(),sessionId:'debug-session',runId:'run1',hypothesisId:'A'})}).catch(()=>{});
    // #endregion
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
        className="flex items-center gap-2 px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-colors"
        aria-label={`Alternar tema. Tema atual: ${getThemeLabel()}`}
        title={`Tema: ${getThemeLabel()} (clique para alternar)`}
        suppressHydrationWarning
      >
        {getThemeIcon()}
        <span 
          className="text-sm font-medium text-gray-700 dark:text-gray-300 hidden sm:inline"
          suppressHydrationWarning
        >
          {getThemeLabel()}
        </span>
      </button>
    </div>
  )
}

