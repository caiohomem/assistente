import { redirect } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { cookies } from "next/headers";
import { NovoContatoClient } from "./NovoContatoClient";
import { TopBar } from "@/components/TopBar";

export default async function NovoContatoPage() {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated) {
    redirect("/login");
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Novo Contato" showBackButton backHref="/dashboard" />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            <NovoContatoClient />
          </div>
        </div>
      </main>
    </div>
  );
}

