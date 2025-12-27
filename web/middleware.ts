import { NextRequest, NextResponse } from "next/server";
import { routing } from "./src/i18n/routing";

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

export default function middleware(req: NextRequest) {
  const res = NextResponse.next();

  const existing = req.cookies.get("NEXT_LOCALE")?.value;
  if (existing && routing.locales.includes(existing as any)) {
    return res;
  }

  const locale = pickLocaleFromAcceptLanguage(req.headers.get("accept-language"));
  res.cookies.set("NEXT_LOCALE", locale, { path: "/", sameSite: "lax" });
  return res;
}

export const config = {
  matcher: ["/((?!api|_next|_vercel|.*\\..*).*)"],
};





