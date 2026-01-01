import { defineRouting } from 'next-intl/routing';
import { createNavigation } from 'next-intl/navigation';

export const routing = defineRouting({
  // A list of all locales that are supported
  locales: ['pt-BR', 'pt-PT', 'en-US', 'es-ES', 'it-IT', 'fr-FR'],

  // Used when no locale matches
  defaultLocale: 'pt-BR',
  
  // Never use locale prefix in URL - detect via cookie
  localePrefix: 'never'
});

// Lightweight wrappers around Next.js' navigation APIs
// that will consider the routing configuration
export const { Link, redirect, usePathname, useRouter } =
  createNavigation(routing);












