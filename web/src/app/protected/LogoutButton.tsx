"use client";

import { useState } from "react";
import { getApiBaseUrl } from "@/lib/bff";

export function LogoutButton() {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const apiBase = getApiBaseUrl();

  async function onLogout() {
    setLoading(true);
    setError(null);
    try {
      window.location.href = `${apiBase}/auth/logout?returnUrl=${encodeURIComponent("/login")}`;
    } catch (e) {
      setError(e instanceof Error ? e.message : "Falha ao fazer logout.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="flex flex-col items-start gap-2">
      <button
        className="rounded-md border border-zinc-300 bg-white px-3 py-1 text-sm hover:bg-zinc-50 disabled:opacity-60"
        onClick={onLogout}
        disabled={loading}
      >
        {loading ? "Saindo..." : "Sair (logout)"}
      </button>
      {error ? <p className="text-sm text-red-700">{error}</p> : null}
    </div>
  );
}


