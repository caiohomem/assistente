import type { NextConfig } from "next";
import createNextIntlPlugin from 'next-intl/plugin';

const withNextIntl = createNextIntlPlugin('./src/i18n/request.ts');

const nextConfig: NextConfig = {
  /* config options here */
  output: 'standalone',
  // NOTA: Não há proxy configurado. As chamadas à API são feitas diretamente
  // usando NEXT_PUBLIC_API_BASE_URL. Se necessário adicionar proxy no futuro,
  // pode-se usar a opção 'rewrites' aqui.
  
  // Permitir requisições cross-origin de hosts públicos (tunnel) para recursos /_next/*
  // Necessário quando o frontend é acessado via tunnel (ex: assistente-web-local.callback-local-cchagas.xyz)
  allowedDevOrigins: [
    'https://assistente-web-local.callback-local-cchagas.xyz',
    'https://assistente-web.callback-local-cchagas.xyz',
    'http://localhost:3000', // Manter localhost para desenvolvimento local
  ],
  
  // Configurações para reduzir refresh automático em desenvolvimento
  reactStrictMode: true,
};

export default withNextIntl(nextConfig);
