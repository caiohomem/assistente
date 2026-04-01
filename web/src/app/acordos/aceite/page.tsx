"use client";

import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";
import { getApiBaseUrl, getN8nWebhookBaseUrl } from "@/lib/bff";

type AcceptanceStatus = "loading" | "pending" | "success" | "failed" | "invalid";

type AcceptancePreview = {
  agreementId: string;
  partyId: string;
  ownerUserId: string;
  maxDays: number;
  agreementTitle: string;
  partyName: string;
  expiresAt: string;
  hasAccepted: boolean;
};

const statusCopy: Record<AcceptanceStatus, { title: string; message: string }> = {
  loading: {
    title: "Validando link",
    message: "Aguarde enquanto confirmamos os dados do aceite.",
  },
  pending: {
    title: "Registrando aceite",
    message: "Estamos confirmando seu aceite. Aguarde alguns instantes.",
  },
  success: {
    title: "Aceite confirmado",
    message: "Seu aceite foi registrado com sucesso.",
  },
  failed: {
    title: "Não foi possível confirmar",
    message: "Ocorreu um erro ao registrar seu aceite. Tente novamente.",
  },
  invalid: {
    title: "Link inválido",
    message: "O link de aceite é inválido ou expirou.",
  },
};

async function fetchPreview(token: string): Promise<AcceptancePreview | null> {
  const apiBaseUrl = getApiBaseUrl();
    console.log("Triggering acceptance webhook 1:", apiBaseUrl);

  const url = `${apiBaseUrl}/api/commission-agreements/acceptance/preview?token=${encodeURIComponent(token)}`;
  const res = await fetch(url, { method: "GET", cache: "no-store" });
  if (!res.ok) {
    return null;
  }
  return (await res.json()) as AcceptancePreview;
} 

async function triggerAcceptanceWebhook(token: string): Promise<boolean> {
  const webhookBaseUrl = getN8nWebhookBaseUrl();
    console.log("Triggering acceptance webhook:", webhookBaseUrl);

  if (!webhookBaseUrl) {
    return false;
  }
  const res = await fetch(`${webhookBaseUrl}/commission-acceptance/confirm`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token }),
  });
  return res.ok;
}

export default function AgreementAcceptancePage() {
  const searchParams = useSearchParams();
  const token = useMemo(() => searchParams?.get("token") ?? "", [searchParams]);
  const [status, setStatus] = useState<AcceptanceStatus>("loading");
  const [preview, setPreview] = useState<AcceptancePreview | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      if (!token) {
        setStatus("invalid");
        return;
      }

      setStatus("loading");
      const previewData = await fetchPreview(token);
        console.log("preview");

      if (cancelled) return;
        console.log("preview2");

      if (!previewData) {
        setStatus("invalid");
        console.log("preview3");

        return;
      }
        console.log("preview4");

      setPreview(previewData);
        console.log("preview6");

      if (previewData.hasAccepted) {
        setStatus("success");
        console.log("preview6");

        return;
      }

      setStatus("pending");
        console.log("previe7");

      const accepted = await triggerAcceptanceWebhook(token);
        console.log("preview8");

      if (cancelled) return;

      setStatus(accepted ? "success" : "failed");
    }

    void load();
    return () => {
      cancelled = true;
    };
  }, [token]);

  const copy = statusCopy[status] ?? statusCopy.success;

  return (
    <main className="min-h-screen bg-zinc-50 dark:bg-zinc-950 text-zinc-900 dark:text-zinc-100 flex items-center justify-center px-6 py-16">
      <div className="w-full max-w-xl rounded-2xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 shadow-sm p-8 space-y-6">
        <div className="space-y-2">
          <p className="text-sm uppercase tracking-wide text-zinc-500 dark:text-zinc-400">
            Aceite de acordo
          </p>
          <h1 className="text-2xl font-semibold">{copy.title}</h1>
          <p className="text-zinc-600 dark:text-zinc-300">{copy.message}</p>
        </div>

        {preview && (
          <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-950/40 p-5 space-y-3">
            <div>
              <p className="text-xs uppercase tracking-wide text-zinc-500">Acordo</p>
              <p className="text-base font-medium text-zinc-900 dark:text-zinc-100">
                {preview.agreementTitle}
              </p>
            </div>
            <div className="grid gap-2 sm:grid-cols-2">
              <div>
                <p className="text-xs uppercase tracking-wide text-zinc-500">Participante</p>
                <p className="text-sm text-zinc-700 dark:text-zinc-200">{preview.partyName}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-zinc-500">Status</p>
                <p className="text-sm text-zinc-700 dark:text-zinc-200">
                  {preview.hasAccepted ? "Aceite confirmado" : "Pendente"}
                </p>
              </div>
            </div>
          </div>
        )}
      </div>
    </main>
  );
}
