"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import { RelationshipType } from "@/lib/types/relationshipType";
import { throwIfErrorResponse } from "./types";

export async function listRelationshipTypesClient(): Promise<RelationshipType[]> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types`, {
    method: "GET",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  await throwIfErrorResponse(res);
  return (await res.json()) as RelationshipType[];
}

export async function createRelationshipTypeClient(name: string): Promise<RelationshipType> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify({ name }),
  });

  await throwIfErrorResponse(res);
  return (await res.json()) as RelationshipType;
}

export async function updateRelationshipTypeClient(id: string, name: string): Promise<RelationshipType> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types/${id}`, {
    method: "PUT",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      "X-CSRF-TOKEN": session.csrfToken,
    },
    body: JSON.stringify({ name }),
  });

  await throwIfErrorResponse(res);
  return (await res.json()) as RelationshipType;
}

export async function deleteRelationshipTypeClient(id: string): Promise<void> {
  const session = await getBffSession();
  if (!session.authenticated || !session.csrfToken) {
    throw new Error("Não autenticado");
  }

  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types/${id}`, {
    method: "DELETE",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": session.csrfToken,
    },
  });

  if (res.status === 204) {
    return;
  }

  await throwIfErrorResponse(res);
}
