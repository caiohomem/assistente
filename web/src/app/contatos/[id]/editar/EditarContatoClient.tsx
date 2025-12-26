"use client";

import { useRouter } from "next/navigation";
import { ContactForm, ContactFormData } from "@/components/ContactForm";
import { 
  updateContactClient, 
  syncContactEmailsClient, 
  syncContactPhonesClient 
} from "@/lib/api/contactsApiClient";

interface EditarContatoClientProps {
  contactId: string;
  initialData: ContactFormData;
}

export function EditarContatoClient({ contactId, initialData }: EditarContatoClientProps) {
  const router = useRouter();

  async function handleSubmit(data: ContactFormData) {
    try {
      // 1. Atualiza os dados básicos do contato (seguindo DDD)
      await updateContactClient(contactId, {
        firstName: data.firstName,
        lastName: data.lastName || null,
        jobTitle: data.jobTitle || null,
        company: data.company || null,
        address: data.address,
      });

      // 2. Sincroniza emails (adiciona novos que não existem, seguindo DDD)
      // O backend valida duplicatas através do método AddEmail da entidade Contact
      const currentEmails = initialData.emails.filter(e => e.trim());
      const newEmails = data.emails.filter(e => e.trim());
      if (newEmails.length > 0) {
        await syncContactEmailsClient(contactId, currentEmails, newEmails);
      }

      // 3. Sincroniza telefones (adiciona novos que não existem, seguindo DDD)
      // O backend valida duplicatas através do método AddPhone da entidade Contact
      const currentPhones = initialData.phones.filter(p => p.trim());
      const newPhones = data.phones.filter(p => p.trim());
      if (newPhones.length > 0) {
        await syncContactPhonesClient(contactId, currentPhones, newPhones);
      }

      // Redireciona para os detalhes do contato após atualizar
      router.push(`/contatos/${contactId}`);
      router.refresh();
    } catch (error) {
      throw error; // Re-throw para o ContactForm tratar
    }
  }

  function handleCancel() {
    router.push(`/contatos/${contactId}`);
  }

  return (
    <ContactForm
      initialData={initialData}
      onSubmit={handleSubmit}
      onCancel={handleCancel}
      submitLabel="Salvar Alterações"
      cancelLabel="Cancelar"
    />
  );
}

