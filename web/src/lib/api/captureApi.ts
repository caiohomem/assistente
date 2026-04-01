import { getBffSession, bffGetJson, getApiBaseUrl } from "../bff";
import { buildApiError } from "./types";
import type {
  CaptureJob,
  UploadCardRequest,
  ProcessAudioNoteRequest,
} from "../types/capture";

async function getSession() {
  const session = await getBffSession();
  if (!session.authenticated || !session.accessToken) {
    throw new Error("Não autenticado");
  }
  return session;
}

async function getAuthorizedHeaders(contentType = true): Promise<HeadersInit> {
  const session = await getSession();
  return {
    ...(contentType ? { "Content-Type": "application/json" } : {}),
    Authorization: `Bearer ${session.accessToken}`,
    ...(session.user?.email ? { "X-User-Email": session.user.email } : {}),
    ...(session.user?.name ? { "X-User-Name": session.user.name } : {}),
  };
}

export interface UploadCardResponse {
  contactId: string;
  jobId: string;
  mediaId: string;
  message: string;
}

export async function uploadCard(request: UploadCardRequest): Promise<UploadCardResponse> {
  const apiBase = getApiBaseUrl();

  const formData = new FormData();
  formData.append("file", request.file);
  if (request.contactId) {
    formData.append("contactId", request.contactId);
  }

  const res = await fetch(`${apiBase}/api/capture/upload-card`, {
    method: "POST",
    credentials: "include",
    headers: await getAuthorizedHeaders(false),
    body: formData,
  });

  if (!res.ok) {
    throw await buildApiError(res);
  }

  return (await res.json()) as UploadCardResponse;
}

export interface ProcessAudioNoteResponse {
  noteId: string;
  jobId: string;
  mediaId: string;
  status: string;
  audioTranscript?: {
    text: string;
    segments?: Array<{
      text: string;
      startTime: string;
      endTime: string;
      confidence: number;
    }> | null;
  } | null;
  audioSummary?: string | null;
  extractedTasks?: Array<{
    description: string;
    dueDate?: string | null;
    priority?: string | null;
  }> | null;
  requestedAt: string;
  completedAt?: string | null;
  errorCode?: string | null;
  errorMessage?: string | null;
  responseMediaId?: string | null;
  message: string;
}

export async function processAudioNote(
  request: ProcessAudioNoteRequest,
): Promise<ProcessAudioNoteResponse> {
  const apiBase = getApiBaseUrl();

  const formData = new FormData();
  formData.append("file", request.file);
  formData.append("contactId", request.contactId);

  const res = await fetch(`${apiBase}/api/capture/audio-note`, {
    method: "POST",
    credentials: "include",
    headers: await getAuthorizedHeaders(false),
    body: formData,
  });

  if (!res.ok) {
    throw await buildApiError(res);
  }

  return (await res.json()) as ProcessAudioNoteResponse;
}

export async function getCaptureJobById(id: string): Promise<CaptureJob> {
  await getSession();
  return bffGetJson<CaptureJob>(`/api/capture/jobs/${id}`);
}

export async function listCaptureJobs(): Promise<CaptureJob[]> {
  await getSession();
  return bffGetJson<CaptureJob[]>("/api/capture/jobs");
}

export interface TranscribeAudioResponse {
  text: string;
  wasTrimmed?: boolean;
  trimmedMessage?: string;
}

export async function transcribeAudio(file: File): Promise<TranscribeAudioResponse> {
  const apiBase = getApiBaseUrl();

  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch(`${apiBase}/api/capture/transcribe-audio`, {
    method: "POST",
    credentials: "include",
    headers: await getAuthorizedHeaders(false),
    body: formData,
  });

  if (!res.ok) {
    throw await buildApiError(res);
  }

  return (await res.json()) as TranscribeAudioResponse;
}
