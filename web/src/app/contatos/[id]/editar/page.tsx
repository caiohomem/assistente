import { redirect, notFound } from "next/navigation";
import { getContactById } from "@/lib/api/contactsApi";
import { getBffSession } from "@/lib/bff";
import { cookies } from "next/headers";
import { EditarContatoClient } from "./EditarContatoClient";
import { TopBar } from "@/components/TopBar";

interface EditarContatoPageProps {
  params: Promise<{ id: string }>;
}

export default async function EditarContatoPage({ params }: EditarContatoPageProps) {
  const { id } = await params;

  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated) {
    redirect("/login");
  }

  let contact;
  try {
    contact = await getContactById(id);
  } catch (error) {
    notFound();
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Editar Contato" showBackButton backHref={`/contatos/${id}`} />
      <main className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto">
          <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
            <EditarContatoClient
              contactId={id}
              initialData={{
                firstName: contact.firstName,
                lastName: contact.lastName || "",
                emails: contact.emails.length > 0 ? contact.emails : [""],
                phones: contact.phones.length > 0 ? contact.phones : [""],
                jobTitle: contact.jobTitle || "",
                company: contact.company || "",
                address: contact.address || {
                  street: "",
                  city: "",
                  state: "",
                  zipCode: "",
                  country: "",
                },
              }}
            />
          </div>
        </div>
      </main>
    </div>
  );
}

