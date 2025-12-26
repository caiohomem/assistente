"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { bffPostJson, getBffSession } from "@/lib/bff";
import { ThemeSelector } from "@/components/ThemeSelector";
import { LanguageSelector } from "@/components/LanguageSelector";

export default function EsqueciSenhaPage() {
  const [email, setEmail] = useState("");
  const [csrfToken, setCsrfToken] = useState<string | null>(null);
  const [status, setStatus] = useState<"idle" | "loading" | "done" | "error">("idle");
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    async function init() {
      try {
        const s = await getBffSession();
        if (!cancelled) setCsrfToken(s.csrfToken);
      } catch {
        if (!cancelled) setMessage("Falha ao obter CSRF via /auth/session.");
      }
    }
    void init();
    return () => {
      cancelled = true;
    };
  }, []);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setStatus("loading");
    setMessage(null);

    try {
      const s = await getBffSession(); // garante cookie XSRF-TOKEN + token atual
      setCsrfToken(s.csrfToken);

      const res = await bffPostJson<{ message: string }>(
        "/auth/forgot-password",
        { email },
        s.csrfToken,
      );
      setStatus("done");
      setMessage(res.message);
    } catch (err) {
      setStatus("error");
      setMessage(err instanceof Error ? err.message : "Erro ao solicitar redefinição.");
    }
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 flex items-center justify-center p-4 relative">
      {/* Language Selector and Theme Selector - Top Right */}
      <div className="absolute top-4 right-4 flex items-center gap-3">
        <LanguageSelector />
        <ThemeSelector />
      </div>

      <div className="max-w-md w-full bg-white dark:bg-gray-800 rounded-2xl shadow-xl p-8">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-2">
            Esqueci minha senha
          </h1>
          <p className="text-gray-600 dark:text-gray-400 text-sm">
            Enviaremos instruções para o email, se ele existir. (O backend retorna sempre OK por anti-enumeração.)
          </p>
        </div>

        {message && (
          <div
            className={`mb-4 p-3 rounded-lg text-sm ${
              status === "error"
                ? "bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300"
                : "bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-700 dark:text-green-300"
            }`}
          >
            {message}
          </div>
        )}

        {status === "done" ? (
          <div className="space-y-4">
            <div className="p-4 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg text-green-700 dark:text-green-300 text-sm">
              <p className="font-medium mb-2">Instruções enviadas!</p>
              <p>
                Se o email informado existir em nossa base de dados, você receberá um email com as instruções para redefinir sua senha.
              </p>
            </div>
            <Link
              href="/login"
              className="block w-full text-center bg-indigo-600 dark:bg-indigo-500 text-white py-3 rounded-lg font-medium hover:bg-indigo-700 dark:hover:bg-indigo-600 transition-colors"
            >
              Voltar ao login
            </Link>
          </div>
        ) : (
          <form className="space-y-4" onSubmit={onSubmit}>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2" htmlFor="email">
                Email
              </label>
              <input
                id="email"
                type="email"
                className="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 px-4 py-3 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent transition-colors"
                placeholder="voce@empresa.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                disabled={status === "loading"}
              />
            </div>

            <button
              type="submit"
              className="w-full bg-indigo-600 dark:bg-indigo-500 text-white py-3 rounded-lg font-medium hover:bg-indigo-700 dark:hover:bg-indigo-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              disabled={status === "loading" || !csrfToken}
              title={!csrfToken ? "Aguardando CSRF via /auth/session" : undefined}
            >
              {status === "loading" ? "Enviando..." : "Enviar instruções"}
            </button>
          </form>
        )}

        {status !== "done" && (
          <div className="mt-6 text-center">
            <Link
              href="/login"
              className="text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 font-medium text-sm"
            >
              Voltar ao login
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}


