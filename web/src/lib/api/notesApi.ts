import { getBffSession, bffGetJson, bffPostJson, bffPutJson } from "../bff";
import { cookies } from "next/headers";
import type {
  Note,
  CreateTextNoteRequest,
  UpdateNoteRequest,
} from "../types/note";

async function getSession(cookieHeader?: string) {
  const session = await getBffSession(cookieHeader ? { cookieHeader } : undefined);
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }
  return session;
}

export async function listNotesByContact(contactId: string): Promise<Note[]> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  await getSession(cookieHeader);
  return bffGetJson<Note[]>(`/api/contacts/${contactId}/notes`, undefined, cookieHeader);
}

export async function getNoteById(id: string): Promise<Note> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore
    .getAll()
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  await getSession(cookieHeader);
  return bffGetJson<Note>(`/api/notes/${id}`, undefined, cookieHeader);
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

  await getSession(cookieHeader);
  return bffPostJson<Note>(`/api/contacts/${contactId}/notes`, request, "", cookieHeader);
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

  await getSession(cookieHeader);
  return bffPutJson<Note>(`/api/notes/${id}`, request, "", cookieHeader);
}
