import { ResetSenhaClient } from "./ResetSenhaClient";

export default async function ResetSenhaPage(props: {
  searchParams?: Promise<{ email?: string; token?: string }>;
}) {
  const searchParams = await props.searchParams;
  return (
    <ResetSenhaClient
      initialEmail={searchParams?.email ?? ""}
      initialToken={searchParams?.token ?? ""}
    />
  );
}


