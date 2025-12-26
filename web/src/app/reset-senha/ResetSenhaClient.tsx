"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { bffPostJson, getBffSession } from "@/lib/bff";
import { ThemeSelector } from "@/components/ThemeSelector";

export function ResetSenhaClient(props: { initialEmail: string; initialToken: string }) {
  const [email, setEmail] = useState(props.initialEmail);
  const [token, setToken] = useState(props.initialToken);
  const [newPassword, setNewPassword] = useState("");
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
        "/auth/reset-password",
        { email, token, newPassword },
        s.csrfToken,
      );
      setStatus("done");
      setMessage(res.message);
    } catch (err) {
      setStatus("error");
      setMessage(err instanceof Error ? err.message : "Erro ao redefinir senha.");
    }
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
      <div className="mx-auto max-w-xl px-6 py-12">
        <div className="flex items-center justify-between mb-8">
          <h1 className="text-2xl font-semibold">Reset de senha</h1>
          <ThemeSelector />
        </div>
        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
          Se você abriu pelo link do email, os campos de email/token já vêm preenchidos.
        </p>

        <form className="mt-8 space-y-4" onSubmit={onSubmit}>
          <div>
            <label className="text-sm font-medium" htmlFor="email">
              Email
            </label>
            <input
              id="email"
              type="email"
              className="mt-1 w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-3 py-2"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </div>

          <div>
            <label className="text-sm font-medium text-zinc-900 dark:text-zinc-100" htmlFor="token">
              Token
            </label>
            <input
              id="token"
              type="text"
              className="mt-1 w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-3 py-2 font-mono text-sm"
              value={token}
              onChange={(e) => setToken(e.target.value)}
              required
            />
          </div>

          <div>
            <label className="text-sm font-medium text-zinc-900 dark:text-zinc-100" htmlFor="newPassword">
              Nova senha
            </label>
            <input
              id="newPassword"
              type="password"
              className="mt-1 w-full rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-3 py-2"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              minLength={8}
            />
            <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">Mínimo 8 caracteres (validação do backend).</p>
          </div>

          <button
            type="submit"
            className="rounded-md bg-black dark:bg-zinc-800 px-4 py-2 text-white hover:bg-zinc-800 dark:hover:bg-zinc-700 disabled:opacity-60"
            disabled={status === "loading" || !csrfToken}
            title={!csrfToken ? "Aguardando CSRF via /auth/session" : undefined}
          >
            {status === "loading" ? "Salvando..." : "Atualizar senha"}
          </button>
        </form>

        {message ? (
          <div
            className={`mt-6 rounded-md border p-4 text-sm ${
              status === "error"
                ? "border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-400"
                : "border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100"
            }`}
          >
            {message}
          </div>
        ) : null}

        <div className="mt-8 flex items-center gap-4 text-sm">
          <Link className="underline" href="/login">
            Voltar ao login
          </Link>
        </div>
      </div>
    </div>
  );
}


