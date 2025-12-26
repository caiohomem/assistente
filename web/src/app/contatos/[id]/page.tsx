import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import Link from "next/link";
import { getBffSession } from "@/lib/bff";
import type { Contact } from "@/lib/types/contact";
import { getContactById } from "@/lib/api/contactsApi";
import { listNotesByContact } from "@/lib/api/notesApi";
import { TopBar } from "@/components/TopBar";
import { ContactDetailsClient } from "./ContactDetailsClient";

export const dynamic = "force-dynamic";

interface ContactDetailsPageProps {
  params: Promise<{ id: string }>;
}

export default async function ContactDetailsPage({ params }: ContactDetailsPageProps) {
  const { id } = await params;
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });

  if (!session.authenticated) {
    redirect(`/login?returnUrl=${encodeURIComponent(`/contatos/${id}`)}`);
  }

  let contact: Contact;
  let notes: Awaited<ReturnType<typeof listNotesByContact>>;

  try {
    [contact, notes] = await Promise.all([
      getContactById(id),
      listNotesByContact(id),
    ]);
  } catch (error) {
    // Log detalhado do erro no servidor
    console.error("[Server] Erro ao carregar contato:", {
      error: error instanceof Error ? error.message : String(error),
      status: (error as any)?.status,
      url: (error as any)?.url,
      stack: error instanceof Error ? error.stack : undefined,
    });

    const errorMessage = error instanceof Error ? error.message : "Erro desconhecido";
    const errorStatus = (error as any)?.status;
    const errorUrl = (error as any)?.url;

    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
        <TopBar title="Erro" showBackButton backHref="/contatos" />
        <main className="container mx-auto px-4 py-8">
          <div className="max-w-4xl mx-auto">
            <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4">
              <h2 className="text-lg font-semibold text-red-900 dark:text-red-400">Erro ao carregar contato</h2>
              <p className="mt-2 text-sm text-red-700 dark:text-red-400">
                {errorMessage}
              </p>
              {errorStatus && (
                <p className="mt-1 text-xs text-red-600 dark:text-red-500">
                  Status: {errorStatus}
                </p>
              )}
              {errorUrl && (
                <p className="mt-1 text-xs text-red-600 dark:text-red-500 break-all">
                  URL: {errorUrl}
                </p>
              )}
              <p className="mt-3 text-xs text-red-600 dark:text-red-500">
                <strong>Nota:</strong> Esta requisição foi feita no servidor (SSR). 
                Verifique os logs do servidor Next.js para mais detalhes. 
                Requisições do servidor não aparecem no Network tab do navegador.
              </p>
              <Link
                href="/contatos"
                className="mt-4 inline-block text-sm text-red-700 dark:text-red-400 underline"
              >
                Voltar para lista de contatos
              </Link>
            </div>
          </div>
        </main>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
      <TopBar title={contact.fullName} showBackButton backHref="/contatos" />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-4xl mx-auto">
          <ContactDetailsClient
            contactId={id}
            contact={contact}
            notes={notes}
          />
        </div>
      </main>
    </div>
  );
}
