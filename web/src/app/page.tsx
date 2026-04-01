'use client'

import Link from "next/link";
import { useTranslations } from 'next-intl';
import { LanguageSelector } from "@/components/LanguageSelector";
import { ThemeSelector } from "@/components/ThemeSelector";
import { PricingCard } from "@/components/PricingCard";
import { listPlans } from "@/lib/api/plansApi";
import type { Plan } from "@/lib/types/plan";
import { useEffect, useState, useMemo } from "react";

type TranslatedPlan = {
  name: string;
  price: string;
  features: string[];
  highlighted?: boolean;
};

type FeatureItem = {
  title: string;
  description: string;
};


export default function Home() {
  const t = useTranslations('landing');
  const [backendPlans, setBackendPlans] = useState<Plan[]>([]);
  const [hasError, setHasError] = useState(false);
  
  // Obter planos traduzidos do JSON
  const translatedPlans = useMemo<TranslatedPlan[]>(() => {
    const raw = t.raw('pricing.plans');
    if (!Array.isArray(raw)) return [];
    return raw.map((plan) => ({
      name: String(plan?.name ?? ""),
      price: String(plan?.price ?? ""),
      features: Array.isArray(plan?.features) ? plan.features.map((feature: unknown) => String(feature ?? "")) : [],
      highlighted: plan?.highlighted ?? false,
    }));
  }, [t]);

  const featureItems = useMemo<FeatureItem[]>(() => {
    const raw = t.raw('features.items');
    if (!Array.isArray(raw)) return [];
    return raw.map((item) => ({
      title: String(item?.title ?? ""),
      description: String(item?.description ?? ""),
    }));
  }, [t]);

  useEffect(() => {
    async function fetchPlans() {
      try {
        const plans = await listPlans();
        if (plans.length > 0) {
          setBackendPlans(plans);
        }
      } catch (error) {
        console.error("Erro ao buscar planos/créditos:", error);
        setHasError(true);
        // Em caso de erro, manter os planos traduzidos do JSON
      }
    }
    fetchPlans();
  }, []);

  const displayPlans = useMemo(() => {
    if (!translatedPlans.length) return [];
    if (!backendPlans.length) return translatedPlans;

    return translatedPlans.map((translatedPlan) => {
      const backendPlan = backendPlans.find(
        (plan) => plan.name?.toLowerCase() === translatedPlan.name.toLowerCase()
      );

      return {
        ...translatedPlan,
        highlighted: backendPlan?.highlighted ?? translatedPlan.highlighted ?? false,
      };
    });
  }, [translatedPlans, backendPlans]);
  return (
    <div className="min-h-screen bg-white dark:bg-gray-900">
      {/* Header */}
      <header className="sticky top-0 z-40 w-full border-b border-gray-200 dark:border-gray-800 bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex h-16 items-center justify-between">
            <div className="flex items-center gap-2">
              <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-500"></div>
              <span className="text-xl font-bold text-gray-900 dark:text-white">
                AssistenteExecutivo
              </span>
            </div>
            
            <nav className="hidden md:flex items-center gap-6">
              <Link
                href="#features"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
              >
                {t('header.navFeatures')}
              </Link>
              <Link
                href="#pricing"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
              >
                {t('header.navPricing')}
              </Link>
              <Link
                href="/login"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
              >
                {t('header.navLogin')}
              </Link>
            </nav>

            <div className="flex items-center gap-4">
              <ThemeSelector />
              <LanguageSelector />
              <Link
                href="/login"
                className="hidden md:inline-flex items-center px-4 py-2 rounded-lg bg-gradient-to-r from-indigo-500 to-purple-500 text-white text-sm font-semibold hover:from-indigo-600 hover:to-purple-600 transition-all shadow-md hover:shadow-lg"
              >
                {t('header.ctaStart')}
              </Link>
            </div>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-indigo-50 via-white to-purple-50 dark:from-gray-900 dark:via-gray-900 dark:to-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-24 sm:py-32">
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-5xl sm:text-6xl font-extrabold text-gray-900 dark:text-white mb-6">
              {t('hero.title')}
              <span className="block text-transparent bg-clip-text bg-gradient-to-r from-indigo-600 to-purple-600">
                {t('hero.titleHighlight')}
              </span>
            </h1>
            <p className="mt-6 text-lg sm:text-xl text-gray-600 dark:text-gray-300 leading-8">
              {t('hero.subtitle')}
            </p>
            <div className="mt-10 flex items-center justify-center gap-4">
              <Link
                href="/login"
                className="inline-flex items-center px-6 py-3 rounded-lg bg-gradient-to-r from-indigo-500 to-purple-500 text-white font-semibold hover:from-indigo-600 hover:to-purple-600 transition-all shadow-lg hover:shadow-xl transform hover:scale-105"
              >
                {t('hero.ctaPrimary')}
              </Link>
              <Link
                href="#features"
                className="inline-flex items-center px-6 py-3 rounded-lg border-2 border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-300 font-semibold hover:border-indigo-500 dark:hover:border-indigo-500 hover:text-indigo-600 dark:hover:text-indigo-400 transition-all"
              >
                {t('hero.ctaSecondary')}
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section id="features" className="py-24 sm:py-32 bg-white dark:bg-gray-900">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-16">
            <h2 className="text-3xl sm:text-4xl font-extrabold text-gray-900 dark:text-white mb-4">
              {t('features.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300">
              {t('features.subtitle')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3">
            {featureItems.map((item, index) => (
              <div key={index} className="relative rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-8 hover:border-indigo-300 dark:hover:border-indigo-600 hover:shadow-lg transition-all">
                <div className="mb-4 h-12 w-12 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-500 flex items-center justify-center">
                  <svg className="h-6 w-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={index === 0 ? "M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" : index === 1 ? "M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" : index === 2 ? "M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" : index === 3 ? "M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" : index === 4 ? "M13 10V3L4 14h7v7l9-11h-7z" : "M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z"} />
                  </svg>
                </div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
                  {item.title}
                </h3>
                <p className="text-gray-600 dark:text-gray-300">
                  {item.description}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="py-24 sm:py-32 bg-gradient-to-br from-gray-50 to-indigo-50 dark:from-gray-900 dark:to-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-16">
            <h2 className="text-3xl sm:text-4xl font-extrabold text-gray-900 dark:text-white mb-4">
              {t('pricing.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300">
              {t('pricing.subtitle')}
            </p>
          </div>

          {hasError && (
            <div className="mb-8 rounded-lg bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 p-4">
              <p className="text-sm text-yellow-800 dark:text-yellow-200">
                {t('pricing.errorLoadingPlans')}
              </p>
            </div>
          )}

          <div className="grid grid-cols-1 gap-8 lg:grid-cols-3 lg:gap-8">
            {displayPlans.map((plan, index) => (
              <PricingCard
                key={plan.name || index}
                name={plan.name}
                price={plan.price}
                features={plan.features}
                highlighted={plan.highlighted}
                popularLabel={plan.highlighted ? t('pricing.popular') : undefined}
                selectPlanLabel={t('pricing.selectPlan')}
                onSelect={() => {
                  // TODO: Implementar seleção de plano
                  console.log(`Plano ${plan.name} selecionado`);
                }}
              />
            ))}
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-100 dark:bg-gray-900 text-gray-700 dark:text-gray-300 border-t border-gray-200 dark:border-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
            <div className="col-span-1 md:col-span-2">
              <div className="flex items-center gap-2 mb-4">
                <div className="h-8 w-8 rounded-lg bg-gradient-to-br from-indigo-500 to-purple-500"></div>
                <span className="text-xl font-bold text-gray-900 dark:text-white">AssistenteExecutivo</span>
              </div>
              <p className="text-sm text-gray-600 dark:text-gray-400 max-w-md">
                {t('footer.description')}
              </p>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-4">{t('footer.product')}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <Link href="#features" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                    {t('footer.navFeatures')}
                  </Link>
                </li>
                <li>
                  <Link href="#pricing" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                    {t('footer.navPricing')}
                  </Link>
                </li>
                <li>
                  <Link href="/login" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                    {t('footer.navLogin')}
                  </Link>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-4">{t('footer.support')}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <a href="#" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                    {t('footer.documentation')}
                  </a>
                </li>
                <li>
                  <a href="#" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                    {t('footer.contact')}
                  </a>
                </li>
                <li>
                  <a href="#" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                    {t('footer.faq')}
                  </a>
                </li>
              </ul>
            </div>
          </div>

          <div className="mt-8 pt-8 border-t border-gray-300 dark:border-gray-800 flex flex-col sm:flex-row justify-between items-center gap-4">
            <p className="text-sm text-gray-600 dark:text-gray-400">
              {t('footer.copyright', { year: new Date().getFullYear() })}
            </p>
            <div className="flex gap-6 text-sm">
              <a href="#" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                {t('footer.terms')}
              </a>
              <a href="#" className="text-gray-600 dark:text-gray-400 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors">
                {t('footer.privacy')}
              </a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
