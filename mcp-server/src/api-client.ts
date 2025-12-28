import axios, { AxiosInstance, AxiosError } from 'axios';
import FormDataNode from 'form-data';

export interface ApiClientConfig {
  baseUrl: string;
  accessToken?: string;
}

export class ApiClient {
  private client: AxiosInstance;
  private accessToken?: string;

  constructor(config: ApiClientConfig) {
    this.accessToken = config.accessToken;
    
    this.client = axios.create({
      baseURL: config.baseUrl.replace(/\/+$/, ''),
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Interceptor para adicionar token de autenticação Bearer
    this.client.interceptors.request.use((requestConfig) => {
      if (this.accessToken) {
        requestConfig.headers.Authorization = `Bearer ${this.accessToken}`;
      }
      return requestConfig;
    });

    // Interceptor para tratamento de erros
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        if (error.response) {
          const status = error.response.status;
          const data = error.response.data as any;
          const message = data?.message || error.message || 'Erro desconhecido';
          
          throw new Error(`API Error (${status}): ${message}`);
        }
        throw error;
      }
    );
  }

  setAccessToken(token: string) {
    // Atualizar token em todas as requisições futuras
    this.accessToken = token;
    this.client.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  clearAccessToken() {
    this.accessToken = undefined;
    delete this.client.defaults.headers.common['Authorization'];
  }

  async get<T>(path: string, params?: Record<string, any>): Promise<T> {
    const response = await this.client.get<T>(path, { params });
    return response.data;
  }

  async post<T>(path: string, data?: any): Promise<T> {
    const response = await this.client.post<T>(path, data);
    return response.data;
  }

  async put<T>(path: string, data?: any): Promise<T> {
    const response = await this.client.put<T>(path, data);
    return response.data;
  }

  async delete<T>(path: string): Promise<T> {
    const response = await this.client.delete<T>(path);
    return response.data;
  }

  async postFormData<T>(path: string, formData: FormDataNode): Promise<T> {
    // Não definir Content-Type manualmente - axios precisa definir o boundary automaticamente
    const response = await this.client.post<T>(path, formData, {
      headers: {
        ...formData.getHeaders(), // Obter headers do FormData (inclui boundary)
      },
      maxContentLength: Infinity,
      maxBodyLength: Infinity,
    });
    return response.data;
  }
}

