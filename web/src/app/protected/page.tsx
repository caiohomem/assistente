import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { TopBar } from "@/components/TopBar";

export const dynamic = "force-dynamic";

export default async function ProtectedPage() {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });

  if (!session.authenticated) {
    redirect(`/login?returnUrl=${encodeURIComponent("/protected")}`);
  }

  // Função para obter iniciais do nome
  const obterIniciais = (name?: string | null, email?: string | null): string => {
    if (name) {
      const partes = name.trim().split(' ')
      const primeiraLetra = partes[0]?.charAt(0)?.toUpperCase() || ''
      const segundaLetra = partes[1]?.charAt(0)?.toUpperCase() || ''
      return primeiraLetra + segundaLetra
    }
    if (email) {
      return email.charAt(0).toUpperCase()
    }
    return 'U'
  }

  // Função para obter cor do avatar baseado no nome/email
  const obterCorAvatar = (texto: string): string => {
    const cores = [
      'bg-indigo-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-red-500',
      'bg-orange-500',
      'bg-yellow-500',
      'bg-green-500',
      'bg-teal-500',
      'bg-blue-500',
      'bg-cyan-500',
    ]
    const index = texto.charCodeAt(0) % cores.length
    return cores[index]
  }

  const user = session.user
  const nomeExibido = user?.name || user?.email || 'Usuário'
  const textoParaCor = user?.name || user?.email || 'U'
  const iniciais = obterIniciais(user?.name, user?.email)
  const corAvatar = obterCorAvatar(textoParaCor)

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900 text-zinc-900 dark:text-zinc-100">
      <TopBar title="Perfil" />
      <div className="mx-auto max-w-4xl px-6 py-8">
        <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
          <h2 className="text-2xl font-semibold text-zinc-900 dark:text-zinc-100 mb-6">
            Perfil do Usuário
          </h2>

          <div className="flex flex-col md:flex-row gap-6 mb-6">
            {/* Avatar */}
            <div className="flex-shrink-0">
              <div className={`h-24 w-24 rounded-full ${corAvatar} flex items-center justify-center text-white text-3xl font-semibold`}>
                {iniciais}
              </div>
            </div>

            {/* Informações básicas */}
            <div className="flex-1">
              <div className="space-y-4">
                <div>
                  <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">
                    Nome
                  </label>
                  <p className="mt-1 text-lg font-semibold text-zinc-900 dark:text-zinc-100">
                    {nomeExibido}
                  </p>
                </div>
                {user?.email && (
                  <div>
                    <label className="text-xs font-medium text-zinc-500 dark:text-zinc-400 uppercase tracking-wide">
                      Email
                    </label>
                    <p className="mt-1 text-base text-zinc-700 dark:text-zinc-300">
                      {user.email}
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
