"use client";

import { useEffect, useMemo, useState } from "react";
import { useAuth, useUser, UserButton } from "@clerk/nextjs";
import { useRouter } from "next/navigation";
import { LayoutWrapper } from "@/components/LayoutWrapper";

export default function ProtectedPage() {
  const { isLoaded, isSignedIn } = useAuth();
  const { user } = useUser();
  const router = useRouter();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isLoaded) {
      return;
    }

    if (!isSignedIn) {
      router.replace(`/login?returnUrl=${encodeURIComponent("/protected")}`);
      return;
    }

    let isMounted = true;

    async function load() {
      try {
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, [isLoaded, isSignedIn, router]);

  const iniciais = useMemo(() => {
    const name = user?.fullName ?? null;
    const email = user?.primaryEmailAddress?.emailAddress ?? null;

    if (name) {
      const partes = name.trim().split(" ");
      const primeiraLetra = partes[0]?.charAt(0)?.toUpperCase() || "";
      const segundaLetra = partes[1]?.charAt(0)?.toUpperCase() || "";
      return primeiraLetra + segundaLetra;
    }
    if (email) {
      return email.charAt(0).toUpperCase();
    }
    return "U";
  }, [user?.fullName, user?.primaryEmailAddress?.emailAddress]);

  const corAvatar = useMemo(() => {
    const texto = user?.fullName || user?.primaryEmailAddress?.emailAddress || "U";
    const cores = [
      "bg-indigo-500",
      "bg-purple-500",
      "bg-pink-500",
      "bg-red-500",
      "bg-orange-500",
      "bg-yellow-500",
      "bg-green-500",
      "bg-teal-500",
      "bg-blue-500",
      "bg-cyan-500",
    ];
    const index = texto.charCodeAt(0) % cores.length;
    return cores[index];
  }, [user?.fullName, user?.primaryEmailAddress?.emailAddress]);

  const nomeExibido = user?.fullName || user?.primaryEmailAddress?.emailAddress || "Usuário";

  return (
    <LayoutWrapper title="Perfil" subtitle="Detalhes do usuário" activeTab="settings">
      <div className="max-w-4xl mx-auto">
        <div className="glass-card p-6">
          <h2 className="text-2xl font-semibold text-foreground mb-6">Perfil do Usuário</h2>

          {loading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
              <span className="ml-3 text-muted-foreground">Carregando...</span>
            </div>
          ) : (
            <div className="flex flex-col md:flex-row gap-6 mb-6">
              <div className="flex-shrink-0">
                <div className={`h-24 w-24 rounded-full ${corAvatar} flex items-center justify-center text-white text-3xl font-semibold`}>
                  {iniciais}
                </div>
              </div>

              <div className="flex-1">
                <div className="space-y-4">
                  <div>
                    <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                      Nome
                    </label>
                    <p className="mt-1 text-lg font-semibold text-foreground">
                      {nomeExibido}
                    </p>
                  </div>
                  {user?.primaryEmailAddress?.emailAddress && (
                    <div>
                      <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                        Email
                      </label>
                      <p className="mt-1 text-base text-muted-foreground">
                        {user.primaryEmailAddress.emailAddress}
                      </p>
                    </div>
                  )}
                  <div className="pt-2">
                    <UserButton />
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </LayoutWrapper>
  );
}
