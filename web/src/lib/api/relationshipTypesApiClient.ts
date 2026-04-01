"use client";

import { getApiBaseUrl, getBffSession } from "@/lib/bff";
import { RelationshipType } from "@/lib/types/relationshipType";
import { throwIfErrorResponse } from "./types";

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

export async function listRelationshipTypesClient(): Promise<RelationshipType[]> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types`, {
    method: "GET",
    credentials: "include",
    headers: await getAuthorizedHeaders(),
  });

  await throwIfErrorResponse(res);
  return (await res.json()) as RelationshipType[];
}

export async function createRelationshipTypeClient(name: string): Promise<RelationshipType> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types`, {
    method: "POST",
    credentials: "include",
    headers: await getAuthorizedHeaders(),
    body: JSON.stringify({ name }),
  });

  await throwIfErrorResponse(res);
  return (await res.json()) as RelationshipType;
}

export async function updateRelationshipTypeClient(id: string, name: string): Promise<RelationshipType> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types/${id}`, {
    method: "PUT",
    credentials: "include",
    headers: await getAuthorizedHeaders(),
    body: JSON.stringify({ name }),
  });

  await throwIfErrorResponse(res);
  return (await res.json()) as RelationshipType;
}

export async function deleteRelationshipTypeClient(id: string): Promise<void> {
  const apiBase = getApiBaseUrl();
  const res = await fetch(`${apiBase}/api/relationship-types/${id}`, {
    method: "DELETE",
    credentials: "include",
    headers: await getAuthorizedHeaders(false),
  });

  if (res.status === 204) {
    return;
  }

  await throwIfErrorResponse(res);
}
