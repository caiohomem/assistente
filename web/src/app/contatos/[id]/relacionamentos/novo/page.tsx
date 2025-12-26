import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { NovoRelacionamentoClient } from "./NovoRelacionamentoClient";
import { TopBar } from "@/components/TopBar";

export const dynamic = "force-dynamic";

interface NovoRelacionamentoPageProps {
  params: Promise<{ id: string }>;
}

export default async function NovoRelacionamentoPage({ params }: NovoRelacionamentoPageProps) {
  const { id } = await params;
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated) {
    redirect(`/login?returnUrl=${encodeURIComponent(`/contatos/${id}/relacionamentos/novo`)}`);
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Novo Relacionamento" showBackButton backHref={`/contatos/${id}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            <NovoRelacionamentoClient contactId={id} />
          </div>
        </div>
      </main>
    </div>
  );
}


