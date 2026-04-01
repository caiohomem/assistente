"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import Link from "next/link";
import { getReminderByIdClient } from "@/lib/api/automationApiClient";
import { EditarLembreteClient } from "./EditarLembreteClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { Button } from "@/components/ui/button";
import { ArrowLeft } from "lucide-react";

export default function EditarLembretePage() {
  const params = useParams<{ id: string }>();
  const reminderId = Array.isArray(params?.id) ? params.id[0] : params?.id;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reminder, setReminder] = useState<Awaited<ReturnType<typeof getReminderByIdClient>> | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      if (!reminderId) return;

      // Validar se o ID é um GUID válido
      const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
      if (!guidRegex.test(reminderId)) {
        if (!isMounted) return;
        setError("ID do lembrete inválido");
        setLoading(false);
        return;
      }

      try {
        const r = await getReminderByIdClient(reminderId);
        if (!isMounted) return;
        setReminder(r);
      } catch (e) {
        console.error("Erro ao carregar lembrete:", e);
        if (!isMounted) return;
        const errorMessage = e instanceof Error ? e.message : "Erro ao carregar lembrete";
        // Se for 404, mostrar mensagem mais específica
        if (errorMessage.includes("404") || errorMessage.includes("não encontrado")) {
          setError("Lembrete não encontrado. Ele pode ter sido deletado ou você não tem permissão para visualizá-lo.");
        } else {
          setError(errorMessage);
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [reminderId]);

  if (!reminderId) return null;

  return (
    <LayoutWrapper
      title="Editar Lembrete"
      subtitle="Atualize o lembrete e o agendamento"
      activeTab="reminders"
    >
      <div className="max-w-2xl mx-auto">
        <div className="mb-6">
          <Link href="/automacao/lembretes">
            <Button variant="ghost" className="gap-2">
              <ArrowLeft className="w-4 h-4" />
              Voltar para lembretes
            </Button>
          </Link>
        </div>
        <div className="glass-card p-6">
          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando lembrete...</span>
            </div>
          ) : error || !reminder ? (
            <div className="rounded-md bg-destructive/10 p-4">
              <p className="text-sm text-destructive">{error ?? "Lembrete não encontrado."}</p>
              <Link
                href="/automacao/lembretes"
                className="mt-3 inline-block text-sm text-destructive underline"
              >
                Voltar
              </Link>
            </div>
          ) : (
            <EditarLembreteClient
              reminderId={reminderId}
              initialData={{
                contactId: reminder.contactId,
                reason: reminder.reason,
                suggestedMessage: reminder.suggestedMessage || "",
                scheduledFor: reminder.scheduledFor,
                status: reminder.status,
              }}
            />
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
