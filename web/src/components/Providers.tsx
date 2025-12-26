'use client'

import { ReactNode } from 'react'
import { ThemeProvider } from '@/lib/theme'
import { NextIntlClientProvider } from 'next-intl'
import type { AbstractIntlMessages } from 'next-intl'

interface ProvidersProps {
  children: ReactNode
  messages: AbstractIntlMessages
  locale: string
}

export function Providers({ children, messages, locale }: ProvidersProps) {
  return (
    <ThemeProvider>
      <NextIntlClientProvider messages={messages} locale={locale} timeZone="America/Sao_Paulo">
        {children}
      </NextIntlClientProvider>
    </ThemeProvider>
  )
}

