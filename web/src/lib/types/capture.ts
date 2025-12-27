export enum JobType {
  CardScan = 1,
  AudioNoteTranscription = 2,
  ProfileEnrichment = 3,
  DueDiligence = 4,
}

export enum JobStatus {
  Requested = 1,
  Processing = 2,
  Succeeded = 3,
  Failed = 4,
  Cancelled = 5,
}

export interface CardScanResult {
  rawText?: string | null;
  name?: string | null;
  email?: string | null;
  phone?: string | null;
  company?: string | null;
  jobTitle?: string | null;
  confidenceScores?: Record<string, number> | null;
  aiRawResponse?: string | null;
}

export interface TranscriptSegment {
  text: string;
  startTime: string; // TimeSpan serialized as string
  endTime: string; // TimeSpan serialized as string
  confidence: number;
}

export interface AudioTranscript {
  text: string;
  segments?: TranscriptSegment[] | null;
}

export interface ExtractedTask {
  description: string;
  dueDate?: string | null;
  priority?: string | null;
}

export interface CaptureJob {
  jobId: string;
  ownerUserId: string;
  type: JobType;
  contactId?: string | null;
  mediaId: string;
  status: JobStatus;
  requestedAt: string;
  completedAt?: string | null;
  errorCode?: string | null;
  errorMessage?: string | null;
  cardScanResult?: CardScanResult | null;
  audioTranscript?: AudioTranscript | null;
  audioSummary?: string | null;
  extractedTasks?: ExtractedTask[] | null;
  responseMediaId?: string | null;
}

export interface UploadCardRequest {
  file: File;
  contactId?: string | null;
}

export interface ProcessAudioNoteRequest {
  file: File;
  contactId: string;
}


