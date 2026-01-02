import { NextRequest, NextResponse } from "next/server";
import { routing } from "./src/i18n/routing";

type AppLocale = (typeof routing.locales)[number];

const isSupportedLocale = (locale: string): locale is AppLocale =>
  routing.locales.includes(locale as AppLocale);

function pickLocaleFromAcceptLanguage(acceptLanguage: string | null): string {
  if (!acceptLanguage) return routing.defaultLocale;

  const normalized = acceptLanguage.toLowerCase();

  const exact = routing.locales.find((locale) =>
    normalized.includes(locale.toLowerCase()),
  );
  if (exact) return exact;

  const baseLang = normalized.split(",")[0]?.split("-")[0]?.trim();
  if (!baseLang) return routing.defaultLocale;

  const baseMatch = routing.locales.find(
    (locale) => locale.toLowerCase().split("-")[0] === baseLang,
  );

  return baseMatch ?? routing.defaultLocale;
}

function getCookieDomain(req: NextRequest): string | undefined {
  // Obter o host da requisição atual
  const hostname = req.headers.get("host") || "";
  
  // Se for localhost ou IP, não configurar domain
  if (hostname === "localhost" || hostname === "127.0.0.1" || /^\d+\.\d+\.\d+\.\d+$/.test(hostname)) {
    return undefined;
  }
  
  // Extrair o domínio base (ex: assistente.live de web.assistente.live)
  const parts = hostname.split(".");
  if (parts.length < 2) {
    return undefined;
  }
  
  // Pegar os últimos 2 ou 3 segmentos (ex: assistente.live ou exemplo.com.br)
  const domainBase = parts.length >= 3 && parts[parts.length - 2].length <= 3
    ? parts.slice(parts.length - 3).join(".") // Para .com.br, .co.uk, etc
    : parts.slice(parts.length - 2).join("."); // Para .com, .live, etc
  
  // Retornar com ponto inicial para funcionar em todos os subdomínios
  return `.${domainBase}`;
}

export default function middleware(req: NextRequest) {
  const res = NextResponse.next();

  const existing = req.cookies.get("NEXT_LOCALE")?.value;
  if (existing && isSupportedLocale(existing)) {
    return res;
  }

  const locale = pickLocaleFromAcceptLanguage(req.headers.get("accept-language"));
  
  // Configurar cookie com domain cross-subdomain se necessário
  const cookieDomain = getCookieDomain(req);
  const isHttps = req.nextUrl.protocol === "https:";
  const cookieOptions: {
    path: string;
    sameSite: "lax" | "strict" | "none";
    secure?: boolean;
    domain?: string;
  } = {
    path: "/",
    sameSite: cookieDomain && isHttps ? "none" : "lax", // SameSite=None requer Secure e HTTPS
    ...(cookieDomain && isHttps && { domain: cookieDomain, secure: true }), // Secure obrigatório com SameSite=None
  };
  
  res.cookies.set("NEXT_LOCALE", locale, cookieOptions);
  return res;
}

export const config = {
  matcher: ["/((?!api|_next|_vercel|.*\\..*).*)"],
};



