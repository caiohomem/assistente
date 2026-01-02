import { Contact, CreateContactRequest, UpdateContactRequest } from "@/lib/types/contact";
import { bffGetJson, bffPostJson, bffPutJson, getBffSession } from "@/lib/bff";
import { cookies } from "next/headers";

export interface ListContactsResult {
  contacts: Contact[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface SearchContactsResult {
  contacts: Contact[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ListContactsParams {
  page?: number;
  pageSize?: number;
  includeDeleted?: boolean;
}

export interface SearchContactsParams {
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

interface RawContactsResponse {
  contacts?: Contact[];
  Contacts?: Contact[];
  total?: number;
  Total?: number;
  page?: number;
  Page?: number;
  pageSize?: number;
  PageSize?: number;
  totalPages?: number;
  TotalPages?: number;
}

/**
 * Lista contatos do usuário autenticado.
 */
export async function listContacts(
  params: ListContactsParams = {},
): Promise<ListContactsResult> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());
  if (params.includeDeleted) queryParams.set("includeDeleted", "true");

  const path = `/api/contacts${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  const data = await bffGetJson<RawContactsResponse>(path, session.csrfToken, cookieHeader);
  // Normalize response to camelCase
  return {
    contacts: data.contacts || data.Contacts || [],
    total: data.total || data.Total || 0,
    page: data.page || data.Page || 1,
    pageSize: data.pageSize || data.PageSize || 20,
    totalPages: data.totalPages || data.TotalPages || 0,
  };
}

/**
 * Busca contatos do usuário autenticado.
 */
export async function searchContacts(
  params: SearchContactsParams = {},
): Promise<SearchContactsResult> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const queryParams = new URLSearchParams();
  if (params.searchTerm) queryParams.set("searchTerm", params.searchTerm);
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const path = `/api/contacts/search${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  const data = await bffGetJson<RawContactsResponse>(path, session.csrfToken, cookieHeader);
  // Normalize response to camelCase
  return {
    contacts: data.contacts || data.Contacts || [],
    total: data.total || data.Total || 0,
    page: data.page || data.Page || 1,
    pageSize: data.pageSize || data.PageSize || 20,
    totalPages: data.totalPages || data.TotalPages || 0,
  };
}

/**
 * Obtém um contato por ID.
 */
export async function getContactById(contactId: string): Promise<Contact> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  return await bffGetJson<Contact>(`/api/contacts/${contactId}`, session.csrfToken, cookieHeader);
}

/**
 * Cria um novo contato.
 */
export async function createContact(
  request: CreateContactRequest,
): Promise<{ contactId: string }> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  // O API só aceita campos básicos no create, emails e phones são adicionados separadamente
  const createRequest = {
    firstName: request.firstName,
    lastName: request.lastName,
    jobTitle: request.jobTitle,
    company: request.company,
    street: request.address?.street,
    city: request.address?.city,
    state: request.address?.state,
    zipCode: request.address?.zipCode,
    country: request.address?.country,
  };

  const result = await bffPostJson<{ contactId: string }>(
    "/api/contacts",
    createRequest,
    session.csrfToken,
    cookieHeader,
  );

  // Adiciona emails se houver
  if (request.emails && request.emails.length > 0) {
    for (const email of request.emails) {
      if (email.trim()) {
        try {
          await bffPostJson<void>(
            `/api/contacts/${result.contactId}/emails`,
            { email: email.trim() },
            session.csrfToken,
            cookieHeader,
          );
        } catch (error) {
          // Log mas não falha a criação do contato
          console.error(`Erro ao adicionar email ${email}:`, error);
        }
      }
    }
  }

  // Adiciona telefones se houver
  if (request.phones && request.phones.length > 0) {
    for (const phone of request.phones) {
      if (phone.trim()) {
        try {
          await bffPostJson<void>(
            `/api/contacts/${result.contactId}/phones`,
            { phone: phone.trim() },
            session.csrfToken,
            cookieHeader,
          );
        } catch (error) {
          // Log mas não falha a criação do contato
          console.error(`Erro ao adicionar telefone ${phone}:`, error);
        }
      }
    }
  }

  return result;
}

/**
 * Atualiza um contato existente.
 */
export async function updateContact(
  contactId: string,
  request: UpdateContactRequest,
): Promise<void> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const session = await getBffSession({ cookieHeader });
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  // O API só aceita campos básicos no update, emails e phones são gerenciados separadamente
  const updateRequest = {
    firstName: request.firstName,
    lastName: request.lastName,
    jobTitle: request.jobTitle,
    company: request.company,
    street: request.address?.street,
    city: request.address?.city,
    state: request.address?.state,
    zipCode: request.address?.zipCode,
    country: request.address?.country,
  };

  await bffPutJson<void>(`/api/contacts/${contactId}`, updateRequest, session.csrfToken, cookieHeader);

  // Nota: Para emails e phones, seria necessário primeiro obter o contato atual,
  // comparar com os novos valores, e adicionar/remover conforme necessário.
  // Por simplicidade, aqui apenas atualizamos os campos básicos.
  // A gestão completa de emails/phones pode ser feita em endpoints separados.
}
