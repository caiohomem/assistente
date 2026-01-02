"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import type { Note, CreateTextNoteRequest, UpdateNoteRequest } from "../types/note";
import { extractApiErrorMessage } from "./types";

/**
 * Lista notas de um contato (client-side).
 */
export async function listNotesByContactClient(contactId: string): Promise<Note[]> {
  const session = await getBffSession();
  if (!session.authenticated) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}/notes`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(session.csrfToken ? { "X-CSRF-TOKEN": session.csrfToken } : {}),
    },
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
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/contacts/${contactId}/notes`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
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
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/notes/${id}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
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
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/notes/${noteId}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (!res.ok) {
    const contentType = res.headers.get("content-type") ?? "";
    const maybeJson = contentType.includes("application/json");
    const data = maybeJson ? await res.json() : undefined;
    const message = extractApiErrorMessage(data) ?? `Request failed: ${res.status}`;
    throw new Error(message);
  }
}
