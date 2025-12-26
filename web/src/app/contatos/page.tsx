import { redirect } from "next/navigation";
import { cookies } from "next/headers";
import { getBffSession } from "@/lib/bff";
import { listContacts } from "@/lib/api/contactsApi";
import { ContactsListClient } from "./ContactsListClient";

export const dynamic = "force-dynamic";

export default async function ContactsPage() {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader }).catch(() => null);

  if (!session?.authenticated) {
    redirect(`/login?returnUrl=${encodeURIComponent("/contatos")}`);
  }

  // Load initial data
  const initialData = await listContacts({ page: 1, pageSize: 20 }).catch(() => ({
    contacts: [],
    total: 0,
    page: 1,
    pageSize: 20,
    totalPages: 0,
  }));

  return <ContactsListClient initialData={initialData} />;
}



