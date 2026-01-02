import type { Metadata } from "next";
import { Geist, Geist_Mono, Outfit } from "next/font/google";
import { getLocale, getMessages } from 'next-intl/server';
import Script from "next/script";
import { Providers } from '@/components/Providers';
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

const outfit = Outfit({
  variable: "--font-outfit",
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700"],
});

export const metadata: Metadata = {
  title: "AssistenteExecutivo Web",
  description: "Web (Next.js) para o AssistenteExecutivo, integrado ao backend BFF.",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale} suppressHydrationWarning>
      <head>
        {/* A-Frame stub to prevent errors when react-force-graph loads VR dependency */}
        <Script id="aframe-stub" strategy="beforeInteractive" dangerouslySetInnerHTML={{
          __html: `
            if (typeof window !== 'undefined' && !window.AFRAME) {
              window.AFRAME = { registerComponent: function() {}, registerSystem: function() {} };
            }
          `,
        }} />
        <Script src="https://aframe.io/releases/1.5.0/aframe.min.js" strategy="beforeInteractive" />
        <Script id="theme-init" strategy="beforeInteractive" dangerouslySetInnerHTML={{
          __html: `
            (function() {
              try {
                const savedTheme = localStorage.getItem('theme');
                const theme = (savedTheme && ['light', 'dark', 'system'].includes(savedTheme)) 
                  ? savedTheme 
                  : 'system';
                
                const root = document.documentElement;
                
                let effectiveTheme;
                if (theme === 'system') {
                  effectiveTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
                } else {
                  effectiveTheme = theme;
                }
                
                root.classList.remove('light');
                root.classList.remove('dark');
                
                if (effectiveTheme === 'dark') {
                  root.classList.add('dark');
                } else {
                  root.classList.remove('dark');
                  if (root.classList.contains('dark')) {
                    root.classList.remove('dark');
                  }
                }
                
                root.setAttribute('data-theme', effectiveTheme);
                void root.offsetHeight;
                
                if (effectiveTheme === 'light' && root.classList.contains('dark')) {
                  root.classList.remove('dark');
                }
                
                if (effectiveTheme === 'light') {
                  let checkCount = 0;
                  const maxChecks = 20;
                  const checkAndRemoveDark = function() {
                    const currentTheme = localStorage.getItem('theme');
                    let shouldBeLight = false;
                    if (!currentTheme || currentTheme === 'light') {
                      shouldBeLight = true;
                    } else if (currentTheme === 'system') {
                      shouldBeLight = !window.matchMedia('(prefers-color-scheme: dark)').matches;
                    }
                    if (shouldBeLight && root.classList.contains('dark')) {
                      root.classList.remove('dark');
                    }
                    checkCount++;
                    if (checkCount < maxChecks) {
                      setTimeout(checkAndRemoveDark, 500);
                    }
                  };
                  setTimeout(checkAndRemoveDark, 100);
                }
              } catch (e) {
                const root = document.documentElement;
                root.classList.remove('light');
                root.classList.remove('dark');
                root.setAttribute('data-theme', 'light');
              }
            })();
          `,
        }} />
      </head>
      <body
        className={`${geistSans.variable} ${geistMono.variable} ${outfit.variable} antialiased`}
        suppressHydrationWarning
      >
        <Providers messages={messages} locale={locale}>
          {children}
        </Providers>
      </body>
    </html>
  );
}
