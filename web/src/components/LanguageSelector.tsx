'use client'

import { useState, useRef, useEffect } from 'react'
import { useLocale } from 'next-intl'

export function LanguageSelector() {
  const locale = useLocale()
  const [isOpen, setIsOpen] = useState(false)
  const [currentLocale, setCurrentLocale] = useState<string>(locale)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const languages = [
    { code: 'pt-BR', flag: '🇧🇷', name: 'Português (BR)' },
    { code: 'pt-PT', flag: '🇵🇹', name: 'Português (PT)' },
    { code: 'en-US', flag: '🇺🇸', name: 'English' },
    { code: 'es-ES', flag: '🇪🇸', name: 'Español' },
    { code: 'it-IT', flag: '🇮🇹', name: 'Italiano' },
    { code: 'fr-FR', flag: '🇫🇷', name: 'Français' },
  ]

  // Sync with locale from next-intl
  useEffect(() => {
    if (locale && languages.some(lang => lang.code === locale)) {
      setCurrentLocale(locale)
    }
  }, [locale])

  const currentLanguage = languages.find(lang => lang.code === currentLocale) || languages[0]

  const changeLanguage = (newLocale: string) => {
    setIsOpen(false)
    setCurrentLocale(newLocale)
    
    // Set the cookie NEXT_LOCALE for persistence
    const expires = new Date()
    expires.setFullYear(expires.getFullYear() + 1)
    document.cookie = `NEXT_LOCALE=${newLocale}; path=/; expires=${expires.toUTCString()}; SameSite=Lax`
    
    // Reload the page to apply the locale change
    // In the future, this can be replaced with next-intl routing
    window.location.reload()
  }

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [])

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-colors"
        aria-label="Selecionar idioma"
      >
        <span className="text-xl">{currentLanguage.flag}</span>
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300 hidden sm:inline">
          {currentLanguage.name}
        </span>
        <svg
          className={`w-4 h-4 text-gray-500 dark:text-gray-400 transition-transform ${isOpen ? 'rotate-180' : ''}`}
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
        </svg>
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-48 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg z-50 overflow-hidden">
          {languages.map((lang) => (
            <button
              key={lang.code}
              onClick={() => changeLanguage(lang.code)}
              className={`w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors ${
                currentLocale === lang.code 
                  ? 'bg-indigo-50 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400' 
                  : 'text-gray-700 dark:text-gray-300'
              }`}
            >
              <span className="text-xl">{lang.flag}</span>
              <span className="text-sm font-medium">{lang.name}</span>
              {currentLocale === lang.code && (
                <svg 
                  className="ml-auto h-4 w-4 text-indigo-600 dark:text-indigo-400" 
                  fill="currentColor" 
                  viewBox="0 0 20 20"
                >
                  <path 
                    fillRule="evenodd" 
                    d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" 
                    clipRule="evenodd" 
                  />
                </svg>
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}

