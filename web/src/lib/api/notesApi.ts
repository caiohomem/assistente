import { getBffSession, bffGetJson, bffPostJson, bffPutJson } from "../bff";
import { cookies } from "next/headers";
import type {
  Note,
  CreateTextNoteRequest,
  UpdateNoteRequest,
} from "../types/note";

async function getCsrfToken(cookieHeader?: string): Promise<string> {
  const session = await getBffSession(cookieHeader ? { cookieHeader } : undefined);
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }
  return session.csrfToken;
}

export async function listNotesByContact(contactId: string): Promise<Note[]> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const csrfToken = await getCsrfToken(cookieHeader);
  return bffGetJson<Note[]>(`/api/contacts/${contactId}/notes`, csrfToken, cookieHeader);
}

export async function getNoteById(id: string): Promise<Note> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const csrfToken = await getCsrfToken(cookieHeader);
  return bffGetJson<Note>(`/api/notes/${id}`, csrfToken, cookieHeader);
}

export async function createTextNote(
  contactId: string,
  request: CreateTextNoteRequest,
): Promise<Note> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const csrfToken = await getCsrfToken(cookieHeader);
  return bffPostJson<Note>(`/api/contacts/${contactId}/notes`, request, csrfToken, cookieHeader);
}

export async function updateNote(
  id: string,
  request: UpdateNoteRequest,
): Promise<Note> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const csrfToken = await getCsrfToken(cookieHeader);
  return bffPutJson<Note>(`/api/notes/${id}`, request, csrfToken, cookieHeader);
}

