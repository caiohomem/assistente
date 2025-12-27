// Cliente principal da API do Assistente Executivo

import { HttpClient } from "./http-client";
import { AuthService } from "./auth";
import { ContactsService } from "./contacts";
import { RemindersService } from "./reminders";
import { NotesService } from "./notes";
import { AutomationService } from "./automation";
import { CreditsService } from "./credits";
import { PlansService } from "./plans";
import { CaptureService } from "./capture";
import { AgentConfigurationService } from "./agent-config";
import { ApiClientConfig, defaultConfig } from "./config";

export class AssistenteExecutivoClient {
  public readonly auth: AuthService;
  public readonly contacts: ContactsService;
  public readonly reminders: RemindersService;
  public readonly notes: NotesService;
  public readonly automation: AutomationService;
  public readonly credits: CreditsService;
  public readonly plans: PlansService;
  public readonly capture: CaptureService;
  public readonly agentConfig: AgentConfigurationService;

  private http: HttpClient;
  private config: ApiClientConfig;

  constructor(config?: Partial<ApiClientConfig>) {
    this.config = { ...defaultConfig, ...config };
    this.http = new HttpClient(this.config);
    
    this.auth = new AuthService(this.http, this.config);
    this.contacts = new ContactsService(this.http);
    this.reminders = new RemindersService(this.http);
    this.notes = new NotesService(this.http);
    this.automation = new AutomationService(this.http);
    this.credits = new CreditsService(this.http);
    this.plans = new PlansService(this.http);
    this.capture = new CaptureService(this.http);
    this.agentConfig = new AgentConfigurationService(this.http);
  }

  /**
   * Define cookies manualmente (útil para integração com sessões existentes)
   */
  setCookies(cookies: Map<string, string>): void {
    this.http.setCookies(cookies);
  }

  /**
   * Obtém cookies atuais
   */
  getCookies(): Map<string, string> {
    return this.http.getCookies();
  }

  /**
   * Obtém a configuração atual
   */
  getConfig(): ApiClientConfig {
    return { ...this.config };
  }
}

// Exportar todos os tipos
export * from "./types";
export * from "./config";
export * from "./agent-config";

// Exportar instância padrão
export default AssistenteExecutivoClient;

