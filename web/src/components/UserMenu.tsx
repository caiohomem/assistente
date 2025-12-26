'use client'

import { useState, useRef, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { getApiBaseUrl, getBffSession, bffDelete } from '@/lib/bff'

interface UserInfo {
  sub?: string | null
  email?: string | null
  name?: string | null
  givenName?: string | null
  familyName?: string | null
}

export function UserMenu() {
  const router = useRouter()
  const apiBase = getApiBaseUrl()
  const [isOpen, setIsOpen] = useState(false)
  const [user, setUser] = useState<UserInfo | null>(null)
  const [loading, setLoading] = useState(true)
  const [logoutLoading, setLogoutLoading] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const carregarUsuario = async () => {
      try {
        const session = await getBffSession()
        if (session.authenticated && session.user) {
          setUser(session.user)
        }
      } catch (error) {
        console.error('Erro ao carregar usuário:', error)
      } finally {
        setLoading(false)
      }
    }

    carregarUsuario()
  }, [])

  // Fechar dropdown ao clicar fora
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

  const handleLogout = async () => {
    setIsOpen(false)
    setLogoutLoading(true)
    try {
      window.location.href = `${apiBase}/auth/logout?returnUrl=${encodeURIComponent('/login')}`
    } catch (error) {
      console.error('Erro ao fazer logout:', error)
      window.location.href = `${apiBase}/auth/logout?returnUrl=${encodeURIComponent('/login')}`
    } finally {
      setLogoutLoading(false)
    }
  }

  const handlePerfil = () => {
    setIsOpen(false)
    router.push('/protected')
  }

  const handleDeleteProfile = async () => {
    if (!showDeleteConfirm) {
      setShowDeleteConfirm(true)
      return
    }

    setDeleteLoading(true)
    try {
      const session = await getBffSession()
      await bffDelete('/api/me', session.csrfToken)
      // Redirecionar para login após exclusão
      window.location.href = `${apiBase}/auth/logout?returnUrl=${encodeURIComponent('/login')}`
    } catch (error) {
      console.error('Erro ao excluir perfil:', error)
      alert('Erro ao excluir perfil. Tente novamente.')
      setShowDeleteConfirm(false)
    } finally {
      setDeleteLoading(false)
    }
  }

  // Função para obter iniciais do nome
  const obterIniciais = (name?: string | null, email?: string | null): string => {
    if (name) {
      const partes = name.trim().split(' ')
      const primeiraLetra = partes[0]?.charAt(0)?.toUpperCase() || ''
      const segundaLetra = partes[1]?.charAt(0)?.toUpperCase() || ''
      return primeiraLetra + segundaLetra
    }
    if (email) {
      return email.charAt(0).toUpperCase()
    }
    return 'U'
  }

  // Função para obter cor do avatar baseado no nome/email
  const obterCorAvatar = (texto: string): string => {
    const cores = [
      'bg-indigo-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-red-500',
      'bg-orange-500',
      'bg-yellow-500',
      'bg-green-500',
      'bg-teal-500',
      'bg-blue-500',
      'bg-cyan-500',
    ]
    const index = texto.charCodeAt(0) % cores.length
    return cores[index]
  }

  if (loading) {
    return (
      <div className="flex items-center gap-2 px-3 py-2">
        <div className="h-8 w-8 rounded-full bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div className="h-4 w-24 bg-gray-200 dark:bg-gray-700 rounded animate-pulse hidden sm:block" />
      </div>
    )
  }

  if (!user) {
    return null
  }

  const nomeExibido = user.name || user.email || 'Usuário'
  const textoParaCor = user.name || user.email || 'U'
  const iniciais = obterIniciais(user.name, user.email)
  const corAvatar = obterCorAvatar(textoParaCor)

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-colors"
        aria-label="Menu do usuário"
        disabled={logoutLoading}
      >
        <div className={`h-8 w-8 rounded-full ${corAvatar} flex items-center justify-center text-white text-sm font-semibold`}>
          {iniciais}
        </div>
        <span className="text-sm font-medium text-gray-700 dark:text-gray-300 hidden sm:inline max-w-[120px] truncate">
          {nomeExibido}
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
        <div className="absolute right-0 mt-2 w-56 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg z-50 overflow-hidden">
          {/* Header do menu com informações do usuário */}
          <div className="px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <div className="flex items-center gap-3">
              <div className={`h-10 w-10 rounded-full ${corAvatar} flex items-center justify-center text-white text-sm font-semibold`}>
                {iniciais}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-gray-900 dark:text-gray-100 truncate">
                  {nomeExibido}
                </p>
                {user.email && (
                  <p className="text-xs text-gray-500 dark:text-gray-400 truncate">
                    {user.email}
                  </p>
                )}
              </div>
            </div>
          </div>

          {/* Opções do menu */}
          <div className="py-1">
            <button
              onClick={handlePerfil}
              className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors text-gray-700 dark:text-gray-300"
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
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
              <span className="text-sm font-medium">Perfil</span>
            </button>
            {showDeleteConfirm ? (
              <div className="px-4 py-3 border-t border-gray-200 dark:border-gray-700">
                <p className="text-xs text-gray-600 dark:text-gray-400 mb-2">
                  Tem certeza que deseja excluir sua conta? Esta ação não pode ser desfeita.
                </p>
                <div className="flex gap-2">
                  <button
                    onClick={handleDeleteProfile}
                    disabled={deleteLoading}
                    className="flex-1 px-3 py-1.5 text-xs font-medium bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    {deleteLoading ? 'Excluindo...' : 'Confirmar'}
                  </button>
                  <button
                    onClick={() => setShowDeleteConfirm(false)}
                    disabled={deleteLoading}
                    className="flex-1 px-3 py-1.5 text-xs font-medium bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                  >
                    Cancelar
                  </button>
                </div>
              </div>
            ) : (
              <button
                onClick={handleDeleteProfile}
                disabled={deleteLoading || logoutLoading}
                className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-red-600 dark:text-red-400 disabled:opacity-50 disabled:cursor-not-allowed"
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
                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                  />
                </svg>
                <span className="text-sm font-medium">Excluir Perfil</span>
              </button>
            )}
            <button
              onClick={handleLogout}
              disabled={logoutLoading || deleteLoading}
              className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-red-600 dark:text-red-400 disabled:opacity-50 disabled:cursor-not-allowed"
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
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
                />
              </svg>
              <span className="text-sm font-medium">
                {logoutLoading ? 'Saindo...' : 'Sair'}
              </span>
            </button>
          </div>
        </div>
      )}
    </div>
  )
}


