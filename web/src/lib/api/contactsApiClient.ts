"use client";

import { Contact, CreateContactRequest, UpdateContactRequest, AddContactRelationshipRequest, Relationship, NetworkGraph } from "@/lib/types/contact";
import { getApiBaseUrl, getBffSession } from "@/lib/bff";

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

/**
 * Lista contatos do usuário autenticado (client-side).
 */
export async function listContactsClient(
  params: ListContactsParams = {},
): Promise<ListContactsResult> {
  const queryParams = new URLSearchParams();
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());
  if (params.includeDeleted) queryParams.set("includeDeleted", "true");

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/contacts${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  
  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  const data = await res.json();
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
 * Busca contatos do usuário autenticado (client-side).
 */
export async function searchContactsClient(
  params: SearchContactsParams = {},
): Promise<SearchContactsResult> {
  const queryParams = new URLSearchParams();
  if (params.searchTerm) queryParams.set("searchTerm", params.searchTerm);
  if (params.page) queryParams.set("page", params.page.toString());
  if (params.pageSize) queryParams.set("pageSize", params.pageSize.toString());

  const apiBase = getApiBaseUrl();
  const path = `${apiBase}/api/contacts/search${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  
  const res = await fetch(path, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  const data = await res.json();
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
 * Obtém um contato por ID (client-side).
 */
export async function getContactByIdClient(contactId: string): Promise<Contact> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return await res.json() as Contact;
}

/**
 * Cria um novo contato (client-side).
 */
export async function createContactClient(
  request: CreateContactRequest,
): Promise<{ contactId: string }> {
  const session = await getBffSession();
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

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(createRequest),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  const result = await res.json() as { contactId: string };

  // Adiciona emails se houver (seguindo DDD - usando métodos da entidade Contact)
  const emailErrors: string[] = [];
  if (request.emails && request.emails.length > 0) {
    for (const email of request.emails) {
      const trimmedEmail = email.trim();
      if (trimmedEmail) {
        try {
          const emailRes = await fetch(`${apiBase}/api/contacts/${result.contactId}/emails`, {
            method: "POST",
            credentials: "include",
            headers: {
              "Content-Type": "application/json",
              "X-CSRF-TOKEN": session.csrfToken,
            },
            body: JSON.stringify({ email: trimmedEmail }),
          });

          if (!emailRes.ok) {
            const errorText = await emailRes.text();
            emailErrors.push(`Erro ao adicionar email ${trimmedEmail}: ${errorText}`);
            console.error(`Erro ao adicionar email ${trimmedEmail}:`, emailRes.status, errorText);
          }
        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : String(error);
          emailErrors.push(`Erro ao adicionar email ${trimmedEmail}: ${errorMessage}`);
          console.error(`Erro ao adicionar email ${trimmedEmail}:`, error);
        }
      }
    }
  }

  // Adiciona telefones se houver (seguindo DDD - usando métodos da entidade Contact)
  const phoneErrors: string[] = [];
  if (request.phones && request.phones.length > 0) {
    for (const phone of request.phones) {
      const trimmedPhone = phone.trim();
      if (trimmedPhone) {
        try {
          const phoneRes = await fetch(`${apiBase}/api/contacts/${result.contactId}/phones`, {
            method: "POST",
            credentials: "include",
            headers: {
              "Content-Type": "application/json",
              "X-CSRF-TOKEN": session.csrfToken,
            },
            body: JSON.stringify({ phone: trimmedPhone }),
          });

          if (!phoneRes.ok) {
            const errorText = await phoneRes.text();
            phoneErrors.push(`Erro ao adicionar telefone ${trimmedPhone}: ${errorText}`);
            console.error(`Erro ao adicionar telefone ${trimmedPhone}:`, phoneRes.status, errorText);
          }
        } catch (error) {
          const errorMessage = error instanceof Error ? error.message : String(error);
          phoneErrors.push(`Erro ao adicionar telefone ${trimmedPhone}: ${errorMessage}`);
          console.error(`Erro ao adicionar telefone ${trimmedPhone}:`, error);
        }
      }
    }
  }

  // Se houver erros críticos, lança exceção
  if (emailErrors.length > 0 || phoneErrors.length > 0) {
    const allErrors = [...emailErrors, ...phoneErrors];
    throw new Error(`Erro ao salvar emails/telefones: ${allErrors.join("; ")}`);
  }

  // Validação final: verificar se pelo menos um email ou telefone foi adicionado com sucesso
  const validEmails = request.emails?.filter(e => e.trim()) || [];
  const validPhones = request.phones?.filter(p => p.trim()) || [];
  
  if (validEmails.length === 0 && validPhones.length === 0) {
    throw new Error("O contato deve ter pelo menos um email ou telefone");
  }

  return result;
}

/**
 * Adiciona um email a um contato (client-side, seguindo DDD).
 */
export async function addContactEmailClient(
  contactId: string,
  email: string,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}/emails`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify({ email: email.trim() }),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

/**
 * Adiciona um telefone a um contato (client-side, seguindo DDD).
 */
export async function addContactPhoneClient(
  contactId: string,
  phone: string,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}/phones`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify({ phone: phone.trim() }),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

/**
 * Sincroniza emails de um contato (adiciona novos que não existem, seguindo DDD).
 * Nota: O backend não possui endpoint para remover emails, então apenas adicionamos novos.
 */
export async function syncContactEmailsClient(
  contactId: string,
  currentEmails: string[],
  newEmails: string[],
): Promise<void> {
  const normalizedCurrent = currentEmails.map(e => e.trim().toLowerCase());
  const emailsToAdd = newEmails
    .map(e => e.trim())
    .filter(e => e && !normalizedCurrent.includes(e.toLowerCase()));

  const errors: string[] = [];
  for (const email of emailsToAdd) {
    try {
      await addContactEmailClient(contactId, email);
    } catch (error) {
      // Se o email já existe, o backend lança exceção - ignoramos silenciosamente
      // Outros erros são coletados
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (!errorMessage.includes("já existe") && !errorMessage.includes("already exists")) {
        errors.push(`Erro ao adicionar email ${email}: ${errorMessage}`);
      }
    }
  }

  if (errors.length > 0) {
    throw new Error(`Erros ao sincronizar emails: ${errors.join("; ")}`);
  }
}

/**
 * Sincroniza telefones de um contato (adiciona novos que não existem, seguindo DDD).
 * Nota: O backend não possui endpoint para remover telefones, então apenas adicionamos novos.
 */
export async function syncContactPhonesClient(
  contactId: string,
  currentPhones: string[],
  newPhones: string[],
): Promise<void> {
  // Normaliza telefones removendo formatação para comparação
  const normalizePhone = (phone: string) => phone.replace(/\D/g, "");
  const normalizedCurrent = currentPhones.map(normalizePhone);
  
  const phonesToAdd = newPhones
    .map(p => p.trim())
    .filter(p => p && !normalizedCurrent.includes(normalizePhone(p)));

  const errors: string[] = [];
  for (const phone of phonesToAdd) {
    try {
      await addContactPhoneClient(contactId, phone);
    } catch (error) {
      // Se o telefone já existe, o backend lança exceção - ignoramos silenciosamente
      // Outros erros são coletados
      const errorMessage = error instanceof Error ? error.message : String(error);
      if (!errorMessage.includes("já existe") && !errorMessage.includes("already exists")) {
        errors.push(`Erro ao adicionar telefone ${phone}: ${errorMessage}`);
      }
    }
  }

  if (errors.length > 0) {
    throw new Error(`Erros ao sincronizar telefones: ${errors.join("; ")}`);
  }
}

/**
 * Atualiza um contato existente (client-side).
 */
export async function updateContactClient(
  contactId: string,
  request: UpdateContactRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  // Transform UpdateContactRequest to match backend format
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

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(updateRequest),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

/**
 * Adiciona um relacionamento a um contato (client-side).
 */
export async function addRelationshipClient(
  contactId: string,
  request: AddContactRelationshipRequest,
): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  // Validar que os IDs estão presentes e são válidos
  if (!contactId || contactId.trim() === "") {
    throw new Error("ID do contato de origem é obrigatório");
  }

  // Validar formato do GUID
  const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  if (!guidRegex.test(contactId)) {
    throw new Error("ID do contato de origem inválido");
  }

  if (!request.targetContactId || request.targetContactId.trim() === "") {
    throw new Error("ID do contato de destino é obrigatório");
  }

  if (!guidRegex.test(request.targetContactId)) {
    throw new Error("ID do contato de destino inválido");
  }

  // Enviar todos os campos do request
  const backendRequest = {
    targetContactId: request.targetContactId,
    type: request.type,
    description: request.description || null,
    strength: request.strength ?? null,
    isConfirmed: request.isConfirmed ?? null,
  };

  const apiBase = getApiBaseUrl();
  // IMPORTANTE: O sourceContactId (contactId) é passado na URL como parâmetro de rota,
  // não no body do request. O backend extrai o sourceContactId de [FromRoute] Guid id
  const url = `${apiBase}/api/contacts/${contactId}/relationships`;
  
  const res = await fetch(url, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify(backendRequest),
  });

  // Endpoint retorna 204 No Content quando bem-sucedido
  if (res.status === 204) {
    return;
  }

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    let data: any = undefined;
    
    // Tentar ler JSON apenas se o content-type indicar JSON
    if (maybeJson) {
      try {
        const text = await res.text();
        if (text && text.trim().length > 0) {
          data = JSON.parse(text);
        }
      } catch (e) {
        // Se falhar ao parsear JSON, usar mensagem padrão
      }
    }
    
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

/**
 * Deleta um relacionamento (client-side).
 */
export async function deleteRelationshipClient(relationshipId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/relationships/${relationshipId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  // Endpoint retorna 204 No Content quando bem-sucedido
  if (res.status === 204) {
    return;
  }

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

/**
 * Deleta um contato (client-side).
 */
export async function deleteContactClient(contactId: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  // Endpoint retorna 204 No Content quando bem-sucedido
  if (res.status === 204) {
    return;
  }

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }
}

/**
 * Obtém o grafo de relacionamentos entre contatos (client-side).
 */
export async function getNetworkGraphClient(maxDepth: number = 2): Promise<NetworkGraph> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const queryParams = new URLSearchParams();
  if (maxDepth) queryParams.set("maxDepth", maxDepth.toString());

  const url = `${apiBase}/api/contacts/network/graph${queryParams.toString() ? `?${queryParams.toString()}` : ""}`;
  
  const res = await fetch(url, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message =
      (data && typeof data === "object" && "message" in data && String((data as any).message)) ||
      `Request failed: ${res.status}`;
    throw new Error(message);
  }

  const data = await res.json();
  
  // Normalize response to camelCase
  return {
    nodes: (data.nodes || data.Nodes || []).map((n: any) => ({
      contactId: n.contactId || n.ContactId,
      fullName: n.fullName || n.FullName,
      company: n.company || n.Company,
      jobTitle: n.jobTitle || n.JobTitle,
      primaryEmail: n.primaryEmail || n.PrimaryEmail,
    })),
    edges: (data.edges || data.Edges || []).map((e: any) => ({
      relationshipId: e.relationshipId || e.RelationshipId,
      sourceContactId: e.sourceContactId || e.SourceContactId,
      targetContactId: e.targetContactId || e.TargetContactId,
      type: e.type || e.Type,
      description: e.description || e.Description,
      strength: e.strength ?? e.Strength ?? 0,
      isConfirmed: e.isConfirmed ?? e.IsConfirmed ?? false,
    })),
  };
}
