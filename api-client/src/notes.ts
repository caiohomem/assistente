import { HttpClient } from "./http-client";
import {
  Note,
  CreateTextNoteRequest,
  UpdateNoteRequest,
} from "./types";

export class NotesService {
  constructor(private http: HttpClient) {}

  /**
   * Lista todas as notas de um contato específico
   */
  async listByContact(contactId: string): Promise<Note[]> {
    return this.http.get<Note[]>(`/api/contacts/${contactId}/notes`);
  }

  /**
   * Obtém uma nota específica por ID
   */
  async getById(noteId: string): Promise<Note> {
    return this.http.get<Note>(`/api/notes/${noteId}`);
  }

  /**
   * Cria uma nova nota de texto para um contato
   */
  async createTextNote(
    contactId: string,
    data: CreateTextNoteRequest
  ): Promise<{ noteId: string }> {
    const response = await this.http.post<string>(
      `/api/contacts/${contactId}/notes`,
      data
    );
    return { noteId: response };
  }

  /**
   * Atualiza uma nota existente
   */
  async update(noteId: string, data: UpdateNoteRequest): Promise<void> {
    return this.http.put(`/api/notes/${noteId}`, data);
  }

  /**
   * Deleta uma nota
   */
  async delete(noteId: string): Promise<void> {
    return this.http.delete(`/api/notes/${noteId}`);
  }

  /**
   * Obtém o arquivo de áudio de uma nota de áudio
   */
  async getAudioFile(noteId: string): Promise<Buffer> {
    const response = await this.http.get<any>(`/api/notes/${noteId}/audio`, undefined, {
      "Accept": "audio/*",
    });
    // node-fetch retorna Response, precisamos converter para Buffer
    if (Buffer.isBuffer(response)) {
      return response;
    }
    return Buffer.from(response);
  }

  /**
   * Obtém um arquivo de mídia por ID
   */
  async getMediaFile(mediaId: string): Promise<Buffer> {
    const response = await this.http.get<any>(`/api/media/${mediaId}/file`, undefined, {
      "Accept": "*/*",
    });
    // node-fetch retorna Response, precisamos converter para Buffer
    if (Buffer.isBuffer(response)) {
      return response;
    }
    return Buffer.from(response);
  }
}

