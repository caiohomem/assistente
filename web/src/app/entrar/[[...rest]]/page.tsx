import { SignIn, SignUp } from "@clerk/nextjs";
import { auth } from "@clerk/nextjs/server";
import { redirect } from "next/navigation";

type EnterPageProps = {
  searchParams?: Promise<{
    returnUrl?: string;
    mode?: string;
  }>;
};

function normalizeReturnUrl(returnUrl?: string): string {
  if (!returnUrl || !returnUrl.startsWith("/")) {
    return "/dashboard";
  }

  return returnUrl;
}

export default async function EntrarPage({ searchParams }: EnterPageProps) {
  const { userId } = await auth();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const returnUrl = normalizeReturnUrl(resolvedSearchParams?.returnUrl);
  const mode = resolvedSearchParams?.mode === "sign-up" ? "sign-up" : "sign-in";

  if (userId) {
    redirect(returnUrl);
  }

  return (
    <main className="min-h-screen flex items-center justify-center bg-background px-4 py-10">
      {mode === "sign-up" ? (
        <SignUp
          path="/entrar"
          routing="path"
          signInUrl={returnUrl ? `/entrar?returnUrl=${encodeURIComponent(returnUrl)}` : "/entrar"}
          fallbackRedirectUrl={returnUrl}
          forceRedirectUrl={returnUrl}
        />
      ) : (
        <SignIn
          path="/entrar"
          routing="path"
          signUpUrl={returnUrl ? `/entrar?mode=sign-up&returnUrl=${encodeURIComponent(returnUrl)}` : "/entrar?mode=sign-up"}
          fallbackRedirectUrl={returnUrl}
          forceRedirectUrl={returnUrl}
        />
      )}
    </main>
  );
}
