import { HttpClient } from "./http-client";
import * as fs from "fs";
import * as path from "path";
import FormData = require("form-data");

export class CaptureService {
  constructor(private http: HttpClient) {}

  /**
   * Upload de cartão de visita para processamento OCR
   */
  async uploadCard(filePath: string): Promise<{
    contactId: string;
    jobId: string;
    mediaId: string;
    message: string;
  }> {
    const formData = new FormData();
    const fileStream = fs.createReadStream(filePath);
    const fileName = path.basename(filePath);
    
    formData.append("file", fileStream, fileName);

    return this.http.post("/api/capture/upload-card", formData);
  }

  /**
   * Processa nota de áudio
   */
  async processAudioNote(
    contactId: string,
    filePath: string
  ): Promise<{
    noteId: string;
    jobId: string;
    mediaId: string;
    status: string;
    audioTranscript?: string;
    audioSummary?: string;
    extractedTasks?: any;
    requestedAt: string;
    completedAt?: string;
    errorCode?: string;
    errorMessage?: string;
    responseMediaId?: string;
    message: string;
    wasTrimmed: boolean;
  }> {
    const formData = new FormData();
    const fileStream = fs.createReadStream(filePath);
    const fileName = path.basename(filePath);
    
    formData.append("contactId", contactId);
    formData.append("file", fileStream, fileName);

    return this.http.post("/api/capture/audio-note", formData);
  }

  /**
   * Obtém job de captura por ID
   */
  async getJobById(jobId: string): Promise<any> {
    return this.http.get(`/api/capture/jobs/${jobId}`);
  }

  /**
   * Lista jobs de captura do usuário
   */
  async listJobs(): Promise<any[]> {
    return this.http.get("/api/capture/jobs");
  }
}

