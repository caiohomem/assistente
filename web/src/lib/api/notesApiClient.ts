"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import type { Note, CreateTextNoteRequest, UpdateNoteRequest } from "../types/note";
import { extractApiErrorMessage } from "./types";

async function getAuthorizedHeaders(contentType = true): Promise<HeadersInit> {
  const session = await getBffSession();
  if (!session.authenticated || !session.accessToken) {
    throw new Error("Não autenticado");
  }

  return {
    ...(contentType ? { "Content-Type": "application/json" } : {}),
    Authorization: `Bearer ${session.accessToken}`,
    ...(session.user?.email ? { "X-User-Email": session.user.email } : {}),
    ...(session.user?.name ? { "X-User-Name": session.user.name } : {}),
  };
}

/**
 * Lista notas de um contato (client-side).
 */
export async function listNotesByContactClient(contactId: string): Promise<Note[]> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}/notes`, {
    method: "GET",
    credentials: "include",
    headers: await getAuthorizedHeaders(),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message = extractApiErrorMessage(data) ?? `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Cria uma nota de texto (client-side).
 */
export async function createTextNoteClient(
  contactId: string,
  request: CreateTextNoteRequest,
): Promise<Note> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}/notes`, {
    method: "POST",
    credentials: "include",
    headers: await getAuthorizedHeaders(),
    body: JSON.stringify(request),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message = extractApiErrorMessage(data) ?? `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Atualiza uma nota (client-side).
 */
export async function updateNoteClient(
  id: string,
  request: UpdateNoteRequest,
): Promise<Note> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/notes/${id}`, {
    method: "PUT",
    credentials: "include",
    headers: await getAuthorizedHeaders(),
    body: JSON.stringify(request),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message = extractApiErrorMessage(data) ?? `Request failed: ${res.status}`;
    throw new Error(message);
  }

  return await res.json();
}

/**
 * Deleta uma nota (client-side).
 */
export async function deleteNoteClient(noteId: string): Promise<void> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/notes/${noteId}`, {
    method: "DELETE",
    credentials: "include",
    headers: await getAuthorizedHeaders(false),
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message = extractApiErrorMessage(data) ?? `Request failed: ${res.status}`;
    throw new Error(message);
  }
}
