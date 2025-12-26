"use client";

import { useRouter } from "next/navigation";
import { ContactForm, ContactFormData } from "@/components/ContactForm";
import { createContactClient } from "@/lib/api/contactsApiClient";

export function NovoContatoClient() {
  const router = useRouter();

  async function handleSubmit(data: ContactFormData) {
    try {
      await createContactClient({
        firstName: data.firstName,
        lastName: data.lastName || null,
        jobTitle: data.jobTitle || null,
        company: data.company || null,
        emails: data.emails.length > 0 ? data.emails : undefined,
        phones: data.phones.length > 0 ? data.phones : undefined,
        address: data.address,
      });

      // Redireciona para a lista de contatos após criar
      router.push("/contatos");
      router.refresh();
    } catch (error) {
      throw error; // Re-throw para o ContactForm tratar
    }
  }

  function handleCancel() {
    router.push("/contatos");
  }

  return (
    <ContactForm
      onSubmit={handleSubmit}
      onCancel={handleCancel}
      submitLabel="Criar Contato"
      cancelLabel="Cancelar"
    />
  );
}

