'use client';

import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { getApiBaseUrl, getBffSession } from '@/lib/bff';

export default function LoginPage() {
  const searchParams = useSearchParams();
  const returnUrl = searchParams?.get('returnUrl') ?? '/dashboard';
  const authError = searchParams?.get('authError');
  const apiBase = getApiBaseUrl();

  const [isChecking, setIsChecking] = useState(true);
  const [hasError, setHasError] = useState(false);
  const hasRedirectedRef = useRef(false);

  useEffect(() => {
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
        // #region agent log
        try {
          const logData = {
            location: 'login/page.tsx:checkAuth',
            message: 'Before getBffSession (client)',
            data: { returnUrl, hasAuthError: !!authError, hasCookies: document.cookie.length > 0 },
            timestamp: Date.now(),
            sessionId: 'debug-session',
            runId: 'run1',
            hypothesisId: 'C',
          };
          await fetch('http://127.0.0.1:7244/ingest/c003d7a1-2df5-4d85-8124-323cc6c30d9d', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(logData),
          }).catch(() => {});
        } catch {}
        // #endregion

        const session = await getBffSession();

        // #region agent log
        try {
          const logData = {
            location: 'login/page.tsx:checkAuth',
            message: 'After getBffSession (client)',
            data: {
              authenticated: session.authenticated,
              hasUser: !!session.user,
              userEmail: session.user?.email,
              hasCsrfToken: !!session.csrfToken,
            },
            timestamp: Date.now(),
            sessionId: 'debug-session',
            runId: 'run1',
            hypothesisId: 'E',
          };
          await fetch('http://127.0.0.1:7244/ingest/c003d7a1-2df5-4d85-8124-323cc6c30d9d', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(logData),
          }).catch(() => {});
        } catch {}
        // #endregion

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
  }, [returnUrl, authError, apiBase]);

  if (isChecking && !hasError) {
    return (
      <main className="min-h-screen flex items-center justify-center p-6">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-300">Verificando autenticação...</p>
        </div>
      </main>
    );
  }

  if (hasError && authError) {
    return (
      <main className="min-h-screen flex items-center justify-center p-6">
        <div className="max-w-md w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 p-6 shadow-sm">
          <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Não foi possível entrar</h1>
          <p className="mt-2 text-sm text-gray-600 dark:text-gray-300">
            Erro: <code className="px-1 py-0.5 rounded bg-gray-100 dark:bg-gray-800">{authError}</code>
          </p>
          <div className="mt-6 flex gap-3">
            <a
              className="inline-flex items-center justify-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
              href={`${apiBase}/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`}
            >
              Tentar novamente
            </a>
            <a
              className="inline-flex items-center justify-center rounded-md border border-gray-300 dark:border-gray-700 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800"
              href="/"
            >
              Voltar
            </a>
          </div>
        </div>
      </main>
    );
  }

  return null;
}
