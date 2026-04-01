"use client";

import { useEffect } from "react";
import Link from "next/link";
import { Show, SignInButton, SignUpButton, UserButton, useAuth } from "@clerk/nextjs";
import { useRouter, useSearchParams } from "next/navigation";

function normalizeReturnUrl(returnUrl: string | null): string {
  if (!returnUrl || !returnUrl.startsWith("/")) {
    return "/dashboard";
  }

  return returnUrl;
}

export default function LoginPage() {
  const { isLoaded, isSignedIn } = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnUrl = normalizeReturnUrl(searchParams?.get("returnUrl") ?? "/dashboard");

  useEffect(() => {
    if (!isLoaded || !isSignedIn) {
      return;
    }

    router.replace(returnUrl);
  }, [isLoaded, isSignedIn, returnUrl, router]);

  return (
    <main className="min-h-screen flex items-center justify-center p-6">
      <div className="max-w-md w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 p-6 shadow-sm">
        <h1 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
          Acesse sua conta
        </h1>
        <p className="mt-2 text-sm text-gray-600 dark:text-gray-300">
          Autenticação do ambiente de desenvolvimento agora roda com Clerk.
        </p>

        <div className="mt-6 flex flex-col gap-3">
          <Show when="signed-out">
            <SignInButton mode="modal" fallbackRedirectUrl={returnUrl} forceRedirectUrl={returnUrl}>
              <button className="inline-flex w-full items-center justify-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700">
                Entrar
              </button>
            </SignInButton>
            <SignUpButton mode="modal" fallbackRedirectUrl={returnUrl} forceRedirectUrl={returnUrl}>
              <button className="inline-flex w-full items-center justify-center rounded-md border border-gray-300 dark:border-gray-700 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-800">
                Criar conta
              </button>
            </SignUpButton>
          </Show>

          <Show when="signed-in">
            <div className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-4">
              <p className="text-sm text-emerald-700 dark:text-emerald-300">
                Sessão ativa. Continue para a aplicação.
              </p>
            </div>
            <div className="flex items-center justify-between">
              <Link
                className="inline-flex items-center justify-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700"
                href={returnUrl}
              >
                Ir para o app
              </Link>
              <UserButton />
            </div>
          </Show>
        </div>

        {!isSignedIn && (
          <p className="mt-4 text-xs text-gray-500 dark:text-gray-400">
            Depois do cadastro, o ícone de perfil vai aparecer no cabeçalho.
          </p>
        )}
      </div>
    </main>
  );
}
