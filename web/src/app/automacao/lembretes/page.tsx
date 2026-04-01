"use client";

import { useEffect, useState, useCallback } from "react";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import { listRemindersClient, type ListRemindersResult } from "@/lib/api/automationApiClient";
import { RemindersListClient } from "@/components/RemindersListClient";

export default function RemindersPage() {
  const [loading, setLoading] = useState(true);
  const [initialData, setInitialData] = useState<ListRemindersResult | undefined>(undefined);

  const loadReminders = useCallback(async () => {
    setLoading(true);
    try {
      const result = await listRemindersClient({
        page: 1,
        pageSize: 20,
      });
      setInitialData(result);
    } catch (err) {
      console.error("Erro ao carregar lembretes:", err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadReminders();
  }, [loadReminders]);

  if (loading) {
    return (
      <LayoutWrapper title="Lembretes" subtitle="Gerencie seus lembretes e agendamentos" activeTab="reminders">
        <div className="flex items-center justify-center py-12">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
            <p className="text-muted-foreground">Carregando lembretes...</p>
          </div>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper 
      title="Lembretes" 
      subtitle="Gerencie seus lembretes e agendamentos"
      activeTab="reminders"
    >
      <RemindersListClient initialData={initialData} />
    </LayoutWrapper>
  );
}
