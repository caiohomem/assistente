'use client'

import { createContext, useContext, useEffect, useState, useCallback, ReactNode } from 'react'

type Theme = 'light' | 'dark' | 'system'

interface ThemeContextType {
  theme: Theme
  resolvedTheme: 'light' | 'dark'
  mounted: boolean
  setTheme: (theme: Theme) => void
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined)

// Função auxiliar para ler o tema do localStorage (deve ser sincronizada com ThemeScript)
function getInitialTheme(): Theme {
  if (typeof window === 'undefined') return 'system'
  
  const savedTheme = localStorage.getItem('theme') as Theme | null
  if (savedTheme && ['light', 'dark', 'system'].includes(savedTheme)) {
    return savedTheme
  }
  return 'system'
}

// Função auxiliar para calcular o tema efetivo (deve ser sincronizada com ThemeScript)
function getEffectiveTheme(currentTheme: Theme): 'light' | 'dark' {
  if (currentTheme === 'system') {
    if (typeof window !== 'undefined') {
      return window.matchMedia('(prefers-color-scheme: dark)').matches
        ? 'dark'
        : 'light'
    }
    return 'light'
  }
  return currentTheme
}

// Função auxiliar para aplicar tema ao DOM (deve ser sincronizada com ThemeScript)
function applyThemeToDOM(currentTheme: Theme): 'light' | 'dark' {
  if (typeof window === 'undefined') return 'light'

  const root = window.document.documentElement
  
  // Calcular o tema efetivo primeiro
  const effectiveTheme = getEffectiveTheme(currentTheme)
  
  // REMOVER TODAS as classes de tema primeiro (garantir estado limpo)
  // Usar múltiplas chamadas para garantir remoção completa
  root.classList.remove('light')
  root.classList.remove('dark')
  
  // Tailwind CSS v4: aplicar classe 'dark' APENAS se o tema efetivo for 'dark'
  // Para tema claro, NÃO adicionar a classe 'dark' (padrão do Tailwind)
  if (effectiveTheme === 'dark') {
    root.classList.add('dark')
  } else {
    // CRÍTICO: Garantir explicitamente que a classe 'dark' seja removida para tema claro
    // Remover múltiplas vezes para garantir que não há classe residual
    root.classList.remove('dark')
    // Verificar novamente e remover se ainda estiver presente
    if (root.classList.contains('dark')) {
      root.classList.remove('dark')
    }
  }
  
  // Atualizar atributo data-theme
  root.setAttribute('data-theme', effectiveTheme)
  
  // Forçar reflow para garantir que o navegador aplique as mudanças
  void root.offsetHeight
  
  // Verificação final: garantir que o estado está correto
  if (effectiveTheme === 'light' && root.classList.contains('dark')) {
    root.classList.remove('dark')
  }
  
  return effectiveTheme
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  // Inicializar estado sincronizado com o que o ThemeScript já aplicou
  // O ThemeScript já aplicou o tema, então lemos o estado atual do DOM
  const [theme, setThemeState] = useState<Theme>(() => {
    if (typeof window === 'undefined') return 'system'
    
    // Sincronizar com o que o ThemeScript já aplicou
    const savedTheme = getInitialTheme()
    return savedTheme
  })
  
  const [resolvedTheme, setResolvedTheme] = useState<'light' | 'dark'>(() => {
    if (typeof window === 'undefined') return 'light'
    
    // Sincronizar com o estado atual do DOM (já aplicado pelo ThemeScript)
    const savedTheme = getInitialTheme()
    return getEffectiveTheme(savedTheme)
  })
  
  const [mounted, setMounted] = useState(false)

  // Aplicar tema ao HTML
  const applyTheme = useCallback((currentTheme: Theme) => {
    if (typeof window === 'undefined') return
    
    const effectiveTheme = applyThemeToDOM(currentTheme)
    setResolvedTheme(effectiveTheme)
  }, [])

  // Inicializar após mount - sincronizar estado com o que o ThemeScript aplicou
  useEffect(() => {
    if (typeof window === 'undefined') return
    
    // O ThemeScript já aplicou o tema, então sincronizamos o estado
    const savedTheme = getInitialTheme()
    const effectiveTheme = getEffectiveTheme(savedTheme)
    
    // Verificar se o DOM já está sincronizado (deve estar, pelo ThemeScript)
    const root = window.document.documentElement
    const currentEffectiveTheme = root.classList.contains('dark') ? 'dark' : 'light'
    
    // Se houver divergência, aplicar o tema correto diretamente
    if (currentEffectiveTheme !== effectiveTheme) {
      const appliedTheme = applyThemeToDOM(savedTheme)
      setResolvedTheme(appliedTheme)
    } else {
      setResolvedTheme(effectiveTheme)
    }
    
    // Sincronizar o estado do tema com o localStorage
    setThemeState(savedTheme)
    
    setMounted(true)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // Executar apenas uma vez no mount

  // Aplicar tema quando mudar (apenas após mount)
  useEffect(() => {
    if (!mounted || typeof window === 'undefined') return

    // Aplicar tema usando a função helper que garante aplicação correta
    const effectiveTheme = applyThemeToDOM(theme)
    
    // Atualizar estado
    setResolvedTheme(effectiveTheme)
    
    // Salvar no localStorage
    localStorage.setItem('theme', theme)
  }, [theme, mounted])

  // Escutar mudanças na preferência do sistema quando tema for 'system'
  useEffect(() => {
    if (!mounted || theme !== 'system' || typeof window === 'undefined') return

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)')
    const handleChange = () => {
      // Recalcular o tema efetivo para 'system'
      const effectiveTheme = getEffectiveTheme('system')
      applyThemeToDOM('system')
      setResolvedTheme(effectiveTheme)
    }

    mediaQuery.addEventListener('change', handleChange)
    return () => mediaQuery.removeEventListener('change', handleChange)
  }, [theme, mounted])

  const setTheme = useCallback((newTheme: Theme) => {
    // Atualizar estado primeiro
    setThemeState(newTheme)
    
    // Aplicar imediatamente no DOM (não esperar pelo useEffect)
    if (typeof window !== 'undefined') {
      const root = window.document.documentElement
      
      
      // Remover classe 'dark' IMEDIATAMENTE se o novo tema for 'light'
      // Isso garante que não haja delay visual
      if (newTheme === 'light') {
        root.classList.remove('dark')
      }
      
      const effectiveTheme = applyThemeToDOM(newTheme)
      setResolvedTheme(effectiveTheme)
      localStorage.setItem('theme', newTheme)
      
      // Verificação final: garantir que está correto
      if (effectiveTheme === 'light' && root.classList.contains('dark')) {
        root.classList.remove('dark')
      }
    }
  }, [theme])

  // Sempre fornecer o contexto, mesmo antes do mount
  return (
    <ThemeContext.Provider value={{ theme, resolvedTheme, mounted, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

export function useTheme() {
  const context = useContext(ThemeContext)
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider')
  }
  return context
}
