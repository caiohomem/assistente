import { redirect } from "next/navigation";

type LoginPageProps = {
  searchParams?: Promise<{
    returnUrl?: string;
  }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const returnUrl = resolvedSearchParams?.returnUrl;

  if (returnUrl && returnUrl.startsWith("/")) {
    redirect(`/entrar?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  redirect("/entrar");
}
