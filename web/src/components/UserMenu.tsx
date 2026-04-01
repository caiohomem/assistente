"use client";

import Link from "next/link";
import { Show, SignInButton, SignUpButton, UserButton, useUser } from "@clerk/nextjs";

export function UserMenu() {
  const { user, isLoaded } = useUser();

  if (!isLoaded) {
    return (
      <div className="flex items-center gap-2 px-3 py-2">
        <div className="h-8 w-8 rounded-full bg-gray-200 dark:bg-gray-700 animate-pulse" />
        <div className="hidden h-4 w-24 rounded bg-gray-200 dark:bg-gray-700 animate-pulse sm:block" />
      </div>
    );
  }

  return (
    <div className="flex items-center gap-2">
      <Show when="signed-out">
        <SignInButton mode="modal">
          <button className="rounded-2xl border border-border/50 bg-secondary/50 px-4 py-2 text-sm font-medium text-foreground transition-all duration-300 hover:bg-secondary/80">
            Entrar
          </button>
        </SignInButton>
        <SignUpButton mode="modal">
          <button className="rounded-2xl bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition-all duration-300 hover:opacity-90">
            Criar conta
          </button>
        </SignUpButton>
      </Show>

      <Show when="signed-in">
        <div className="hidden text-right sm:block">
          <p className="max-w-[140px] truncate text-sm font-medium text-foreground">
            {user?.fullName || user?.primaryEmailAddress?.emailAddress || "Usuário"}
          </p>
          <Link href="/protected" className="text-xs text-muted-foreground hover:text-foreground">
            Perfil
          </Link>
        </div>
        <UserButton />
      </Show>
    </div>
  );
}
