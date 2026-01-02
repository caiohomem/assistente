import { getBffSession, bffGetJson, getApiBaseUrl } from "../bff";
import { buildApiError } from "./types";
import type {
  CaptureJob,
  UploadCardRequest,
  ProcessAudioNoteRequest,
} from "../types/capture";

async function getCsrfToken(): Promise<string> {
  const session = await getBffSession();
  return session.csrfToken;
}

const handleUnauthorizedRedirect = (res: Response): boolean => {
  if (res.status === 401 && typeof window !== "undefined") {
    const currentPath = window.location.pathname;
    const loginUrl = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
    window.location.href = loginUrl;
    return true;
  }
  return false;
};

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
    if (handleUnauthorizedRedirect(res)) {
      return {} as UploadCardResponse;
    }
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
    if (handleUnauthorizedRedirect(res)) {
      return {} as ProcessAudioNoteResponse;
    }
    throw await buildApiError(res);
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

export interface TranscribeAudioResponse {
  text: string;
  wasTrimmed?: boolean;
  trimmedMessage?: string;
}

export async function transcribeAudio(file: File): Promise<TranscribeAudioResponse> {
  const csrfToken = await getCsrfToken();
  const apiBase = getApiBaseUrl();

  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch(`${apiBase}/api/capture/transcribe-audio`, {
    method: "POST",
    credentials: "include",
    headers: {
      "X-CSRF-TOKEN": csrfToken,
    },
    body: formData,
  });

  if (!res.ok) {
    if (handleUnauthorizedRedirect(res)) {
      return {} as TranscribeAudioResponse;
    }
    throw await buildApiError(res);
  }

  return (await res.json()) as TranscribeAudioResponse;
}
