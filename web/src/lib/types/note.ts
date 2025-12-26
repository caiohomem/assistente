export enum NoteType {
  Audio = 1,
  Text = 2,
}

export interface Note {
  noteId: string;
  contactId: string;
  authorId: string;
  type: NoteType;
  rawContent: string;
  structuredData?: string | null;
  version: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTextNoteRequest {
  text: string;
  structuredData?: string | null;
}

export interface UpdateNoteRequest {
  rawContent: string;
  structuredData?: string | null;
}

