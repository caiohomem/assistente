import { getBffSession, bffGetJson, getApiBaseUrl } from "../bff";
import type {
  CaptureJob,
  UploadCardRequest,
  ProcessAudioNoteRequest,
} from "../types/capture";

async function getCsrfToken(): Promise<string> {
  const session = await getBffSession();
  return session.csrfToken;
}

export interface UploadCardResponse {
  contactId: string;
  jobId: string;
  mediaId: string;
  message: string;
}

export async function uploadCard(request: UploadCardRequest): Promise<UploadCardResponse> {
  const csrfToken = await getCsrfToken();
  const apiBase = getApiBaseUrl();

  const formData = new FormData();
  formData.append("file", request.file);
  if (request.contactId) {
    formData.append("contactId", request.contactId);
  }

  const res = await fetch(`${apiBase}/api/capture/upload-card`, {
    method: "POST",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": csrfToken,
    },
    body: formData,
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
  message: string;
}

export async function processAudioNote(
  request: ProcessAudioNoteRequest,
): Promise<ProcessAudioNoteResponse> {
  const csrfToken = await getCsrfToken();
  const apiBase = getApiBaseUrl();

  const formData = new FormData();
  formData.append("file", request.file);
  formData.append("contactId", request.contactId);

  const res = await fetch(`${apiBase}/api/capture/audio-note`, {
    method: "POST",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": csrfToken,
    },
    body: formData,
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

  return (await res.json()) as ProcessAudioNoteResponse;
}

export async function getCaptureJobById(id: string): Promise<CaptureJob> {
  const csrfToken = await getCsrfToken();
  return bffGetJson<CaptureJob>(`/api/capture/jobs/${id}`, csrfToken);
}

export async function listCaptureJobs(): Promise<CaptureJob[]> {
  const csrfToken = await getCsrfToken();
  return bffGetJson<CaptureJob[]>("/api/capture/jobs", csrfToken);
}

