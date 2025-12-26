import { redirect } from 'next/navigation';
import { getApiBaseUrl } from '@/lib/bff';

export default async function RegistroPage() {
  const apiBase = getApiBaseUrl();
  
  // Redirecionar imediatamente para Keycloak via BFF com ação de registro
  redirect(`${apiBase}/auth/register`);
}
