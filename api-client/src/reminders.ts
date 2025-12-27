import { HttpClient } from "./http-client";
import {
  Reminder,
  CreateReminderRequest,
  UpdateReminderStatusRequest,
  ReminderStatus,
  ListResult,
} from "./types";

export class RemindersService {
  constructor(private http: HttpClient) {}

  /**
   * Lista lembretes do usuário autenticado
   */
  async list(options?: {
    contactId?: string;
    status?: ReminderStatus;
    startDate?: Date;
    endDate?: Date;
    page?: number;
    pageSize?: number;
  }): Promise<ListResult<Reminder>> {
    const params: any = {
      page: options?.page || 1,
      pageSize: options?.pageSize || 20,
    };

    if (options?.contactId) {
      params.contactId = options.contactId;
    }
    if (options?.status) {
      params.status = options.status;
    }
    if (options?.startDate) {
      params.startDate = options.startDate.toISOString();
    }
    if (options?.endDate) {
      params.endDate = options.endDate.toISOString();
    }

    return this.http.get<ListResult<Reminder>>("/api/automation/reminders", params);
  }

  /**
   * Obtém um lembrete específico por ID
   */
  async getById(reminderId: string): Promise<Reminder> {
    return this.http.get<Reminder>(`/api/automation/reminders/${reminderId}`);
  }

  /**
   * Cria um novo lembrete
   */
  async create(data: CreateReminderRequest): Promise<{ reminderId: string }> {
    const response = await this.http.post<string>(
      "/api/automation/reminders",
      data
    );
    return { reminderId: response };
  }

  /**
   * Atualiza o status de um lembrete
   */
  async updateStatus(
    reminderId: string,
    data: UpdateReminderStatusRequest
  ): Promise<void> {
    return this.http.put(`/api/automation/reminders/${reminderId}/status`, data);
  }

  /**
   * Deleta um lembrete
   */
  async delete(reminderId: string): Promise<void> {
    return this.http.delete(`/api/automation/reminders/${reminderId}`);
  }
}

