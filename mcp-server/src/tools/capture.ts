import { Tool } from '@modelcontextprotocol/sdk/types.js';
import { ApiClient } from '../api-client.js';
import FormDataNode from 'form-data';
import { Readable } from 'stream';

export function createCaptureTools(apiClient: ApiClient): Tool[] {
  return [
    {
      name: 'get_capture_job',
      description: 'Obtém um job de captura por ID',
      inputSchema: {
        type: 'object',
        properties: {
          id: {
            type: 'string',
            description: 'ID do job (GUID)',
          },
        },
        required: ['id'],
      },
    },
    {
      name: 'list_capture_jobs',
      description: 'Lista jobs de captura do usuário autenticado',
      inputSchema: {
        type: 'object',
        properties: {},
      },
    },
    {
      name: 'create_audio_note',
      description: 'Cria uma nova nota de áudio para um contato. O arquivo de áudio deve ser fornecido como base64.',
      inputSchema: {
        type: 'object',
        properties: {
          contactId: {
            type: 'string',
            description: 'ID do contato (GUID)',
          },
          audioBase64: {
            type: 'string',
            description: 'Arquivo de áudio codificado em base64',
          },
          fileName: {
            type: 'string',
            description: 'Nome do arquivo (ex: audio.mp3, recording.wav). Se não fornecido, será usado "audio-note"',
          },
          mimeType: {
            type: 'string',
            description: 'Tipo MIME do áudio (ex: audio/mpeg, audio/wav, audio/webm). Formatos suportados: MP3, MP4, WAV, WebM, M4A, OGG',
          },
        },
        required: ['contactId', 'audioBase64', 'mimeType'],
      },
    },
  ];
}

export async function handleCaptureTool(
  name: string,
  args: any,
  apiClient: ApiClient
): Promise<any> {
  switch (name) {
    case 'get_capture_job':
      return await apiClient.get(`/api/capture/jobs/${args.id}`);

    case 'list_capture_jobs':
      return await apiClient.get('/api/capture/jobs');

    case 'create_audio_note': {
      // Converter base64 para Buffer
      const audioBuffer = Buffer.from(args.audioBase64, 'base64');
      
      // Criar FormData
      const formData = new FormDataNode();
      formData.append('contactId', args.contactId);
      
      // Criar um stream a partir do buffer para o FormData
      const audioStream = Readable.from(audioBuffer);
      formData.append('file', audioStream, {
        filename: args.fileName || 'audio-note',
        contentType: args.mimeType,
      });

      // Fazer upload usando postFormData
      return await apiClient.postFormData('/api/capture/audio-note', formData);
    }

    default:
      throw new Error(`Ferramenta desconhecida: ${name}`);
  }
}


