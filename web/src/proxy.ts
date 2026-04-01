import { clerkMiddleware } from "@clerk/nextjs/server";
import { NextRequest, NextResponse } from "next/server";
import { routing } from "./i18n/routing";

type AppLocale = (typeof routing.locales)[number];

const isSupportedLocale = (locale: string): locale is AppLocale =>
  routing.locales.includes(locale as AppLocale);

function pickLocaleFromAcceptLanguage(acceptLanguage: string | null): string {
  if (!acceptLanguage) return routing.defaultLocale;

  const normalized = acceptLanguage.toLowerCase();
  const exact = routing.locales.find((locale) => normalized.includes(locale.toLowerCase()));
  if (exact) return exact;

  const baseLang = normalized.split(",")[0]?.split("-")[0]?.trim();
  if (!baseLang) return routing.defaultLocale;

  const baseMatch = routing.locales.find(
    (locale) => locale.toLowerCase().split("-")[0] === baseLang,
  );

  return baseMatch ?? routing.defaultLocale;
}

function getCookieDomain(req: NextRequest): string | undefined {
  const hostname = req.headers.get("host") || "";

  if (
    hostname === "localhost" ||
    hostname === "127.0.0.1" ||
    /^\d+\.\d+\.\d+\.\d+$/.test(hostname)
  ) {
    return undefined;
  }

  const parts = hostname.split(".");
  if (parts.length < 2) {
    return undefined;
  }

  const domainBase =
    parts.length >= 3 && parts[parts.length - 2].length <= 3
      ? parts.slice(parts.length - 3).join(".")
      : parts.slice(parts.length - 2).join(".");

  return `.${domainBase}`;
}

export default clerkMiddleware(async (_auth, req) => {
  const res = NextResponse.next();

  const existing = req.cookies.get("NEXT_LOCALE")?.value;
  if (existing && isSupportedLocale(existing)) {
    return res;
  }

  const locale = pickLocaleFromAcceptLanguage(req.headers.get("accept-language"));
  const cookieDomain = getCookieDomain(req);
  const isHttps = req.nextUrl.protocol === "https:";

  res.cookies.set("NEXT_LOCALE", locale, {
    path: "/",
    sameSite: cookieDomain && isHttps ? "none" : "lax",
    ...(cookieDomain && isHttps ? { domain: cookieDomain, secure: true } : {}),
  });

  return res;
});

export const config = {
  matcher: [
    "/((?!_next|[^?]*\\.(?:html?|css|js(?!on)|jpe?g|webp|png|gif|svg|ttf|woff2?|ico|csv|docx?|xlsx?|zip|webmanifest)).*)",
    "/(api|trpc)(.*)",
  ],
};
