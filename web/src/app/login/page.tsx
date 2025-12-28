'use client';

import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { useTranslations } from 'next-intl';
import { getApiBaseUrl, getBffSession } from '@/lib/bff';

export default function LoginPage() {
  const t = useTranslations('login');
  const searchParams = useSearchParams();
  const returnUrl = searchParams?.get('returnUrl') ?? '/dashboard';
  const authError = searchParams?.get('authError');
  const email = searchParams?.get('email');
  const keycloakSubject = searchParams?.get('keycloakSubject');
  const apiBase = getApiBaseUrl();

  const [isChecking, setIsChecking] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [isReactivating, setIsReactivating] = useState(false);
  const hasRedirectedRef = useRef(false);

  useEffect(() => {
    // Se já temos erro de usuário deletado com email e keycloakSubject, não fazer nada
    if (authError === 'usuario_deletado' && email && keycloakSubject) {
      setHasError(true);
      setIsChecking(false);
      return;
    }

    const redirectKey = 'login_redirect_attempt';
    const lastRedirect = sessionStorage.getItem(redirectKey);
    const now = Date.now();

    if (lastRedirect && now - parseInt(lastRedirect, 10) < 2000) {
      console.log('[Login] Redirecionamento recente detectado, aguardando...');
      return;
    }

    if (hasRedirectedRef.current) return;

    let isMounted = true;

    async function checkAuth() {
      try {
        const session = await getBffSession();

        if (!isMounted) return;

        if (session.authenticated && session.user?.email) {
          if (hasRedirectedRef.current) return;
          hasRedirectedRef.current = true;
          sessionStorage.setItem(redirectKey, now.toString());

          console.log('[Login] Usuário já autenticado, redirecionando para:', returnUrl);
          window.location.href = returnUrl;
          return;
        }

        if (!authError) {
          if (hasRedirectedRef.current) return;
          hasRedirectedRef.current = true;
          sessionStorage.setItem(redirectKey, now.toString());

          window.location.href = `${apiBase}/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`;
          return;
        }

        setHasError(true);
        setIsChecking(false);
      } catch (error) {
        console.error('[Login] Erro ao verificar sessão:', error);
        if (!isMounted) return;

        if (!authError) {
          if (hasRedirectedRef.current) return;
          hasRedirectedRef.current = true;
          sessionStorage.setItem(redirectKey, now.toString());

          window.location.href = `${apiBase}/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`;
          return;
        }

        setHasError(true);
        setIsChecking(false);
      }
    }

    checkAuth();

    return () => {
      isMounted = false;
    };
  }, [returnUrl, authError, apiBase, email, keycloakSubject]);

  if (isChecking && !hasError) {
    return (
      <main className="min-h-screen flex items-center justify-center p-6">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-300">{t('checkingAuth')}</p>
        </div>
      </main>
    );
  }

  // Se for erro de usuário deletado e temos email/keycloakSubject, mostrar pergunta de reativação
  if (hasError && authError === 'usuario_deletado' && email && keycloakSubject) {
    const handleReactivate = async () => {
      setIsReactivating(true);
      try {
        const response = await fetch(`${apiBase}/auth/reactivate`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          credentials: 'include',
          body: JSON.stringify({
            email,
            keycloakSubject,
          }),
        });

        if (!response.ok) {
          const error = await response.json().catch(() => ({ message: t('reactivateError') }));
          alert(error.message || t('reactivateError'));
          setIsReactivating(false);
          return;
        }

        // Usuário reativado com sucesso, redirecionar para login
        window.location.href = `${apiBase}/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`;
      } catch (error) {
        console.error('[Login] Erro ao reativar usuário:', error);
        alert(t('reactivateErrorRetry'));
        setIsReactivating(false);
      }
    };

    return (
      <main className="min-h-screen flex items-center justify-center p-6">
        <div className="max-w-md w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 p-6 shadow-sm">
          <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100 mb-4">
            {t('reactivateTitle')}
          </h1>
          <p className="mt-2 text-sm text-gray-600 dark:text-gray-300 mb-6">
            {t('reactivateDescription')}
          </p>
          <div className="mt-6 flex gap-3">
            <button
              onClick={handleReactivate}
              disabled={isReactivating}
              className="inline-flex items-center justify-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isReactivating ? t('reactivating') : t('reactivateButton')}
            </button>
            <a
              className="inline-flex items-center justify-center rounded-md border border-gray-300 dark:border-gray-700 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800"
              href="/"
            >
              {t('goBack')}
            </a>
          </div>
        </div>
      </main>
    );
  }

  if (hasError && authError) {
    return (
      <main className="min-h-screen flex items-center justify-center p-6">
        <div className="max-w-md w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 p-6 shadow-sm">
          <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{t('errorTitle')}</h1>
          <p className="mt-2 text-sm text-gray-600 dark:text-gray-300">
            {t('errorLabel')} <code className="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-800">{authError}</code>
          </p>
          <div className="mt-6 flex gap-3">
            <a
              className="inline-flex items-center justify-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
              href={`${apiBase}/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`}
            >
              {t('tryAgain')}
            </a>
            <a
              className="inline-flex items-center justify-center rounded-md border border-gray-300 dark:border-gray-700 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800"
              href="/"
            >
              {t('back')}
            </a>
          </div>
        </div>
      </main>
    );
  }

  return null;
}
