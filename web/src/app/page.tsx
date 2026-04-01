'use client'

import Link from "next/link";
import { useTranslations } from 'next-intl';
import { LanguageSelector } from "@/components/LanguageSelector";
import { ThemeSelector } from "@/components/ThemeSelector";
import { PricingCard } from "@/components/PricingCard";
import { listPlans } from "@/lib/api/plansApi";
import type { Plan } from "@/lib/types/plan";
import { useEffect, useState, useMemo } from "react";
import { LayoutDashboard, Zap, Brain, Mic, MessageSquare, FileText } from "lucide-react";

type TranslatedPlan = {
  name: string;
  price: string;
  features: string[];
  highlighted?: boolean;
  tagline?: string;
  description?: string;
};

export default function Home() {
  const t = useTranslations('landing');
  const [backendPlans, setBackendPlans] = useState<Plan[]>([]);
  const [hasError, setHasError] = useState(false);

  const translatedPlans = useMemo<TranslatedPlan[]>(() => {
    const raw = t.raw('pricing.plans');
    if (!Array.isArray(raw)) return [];
    return raw.map((plan) => ({
      name: String(plan?.name ?? ""),
      price: String(plan?.price ?? ""),
      features: Array.isArray(plan?.features) ? plan.features.map((feature: unknown) => String(feature ?? "")) : [],
      highlighted: plan?.highlighted ?? false,
      tagline: String(plan?.tagline ?? ""),
      description: String(plan?.description ?? ""),
    }));
  }, [t]);

  const problemItems = useMemo(() => {
    const raw = t.raw('problem.items');
    if (!Array.isArray(raw)) return [];
    return raw.map((item) => ({
      title: String(item?.title ?? ""),
      description: String(item?.description ?? ""),
      icon: String(item?.icon ?? ""),
    }));
  }, [t]);

  const solutionPillars = useMemo(() => {
    const raw = t.raw('solution.pillars');
    if (!Array.isArray(raw)) return [];
    return raw.map((pillar) => ({
      title: String(pillar?.title ?? ""),
      description: String(pillar?.description ?? ""),
      benefits: Array.isArray(pillar?.benefits) ? pillar.benefits.map((b: unknown) => String(b ?? "")) : [],
      icon: String(pillar?.icon ?? ""),
    }));
  }, [t]);

  const caseStudies = useMemo(() => {
    const raw = t.raw('proof.caseStudies');
    if (!Array.isArray(raw)) return [];
    return raw.map((cs) => ({
      company: String(cs?.company ?? ""),
      role: String(cs?.role ?? ""),
      result: String(cs?.result ?? ""),
      quote: String(cs?.quote ?? ""),
      author: String(cs?.author ?? ""),
    }));
  }, [t]);

  const workflowSteps = useMemo(() => {
    const raw = t.raw('workflow.steps');
    if (!Array.isArray(raw)) return [];
    return raw.map((step) => ({
      number: String(step?.number ?? ""),
      title: String(step?.title ?? ""),
      description: String(step?.description ?? ""),
    }));
  }, [t]);

  const aiFeatures = useMemo(() => {
    const raw = t.raw('aiFeatures.features');
    if (!Array.isArray(raw)) return [];
    return raw.map((feature) => ({
      name: String(feature?.name ?? ""),
      description: String(feature?.description ?? ""),
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
        console.error("Erro ao buscar planos:", error);
        setHasError(true);
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

  const getIconSvg = (iconName: string) => {
    const icons: Record<string, string> = {
      network: "M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z",
      clock: "M12 8v4l3 2m6-2a9 9 0 11-18 0 9 9 0 0118 0z",
      alert: "M12 8v4m0 4v.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z",
      brain: "M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5a4 4 0 100-8 4 4 0 000 8z",
      zap: "M13 10V3L4 14h7v7l9-11h-7z",
      shield: "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z",
    };
    return icons[iconName] || icons.network;
  };

  const getAIFeatureIcon = (featureName: string) => {
    const iconMap: Record<string, React.ComponentType<{ className: string }>> = {
      'Chat Assistente': MessageSquare,
      'Chat Assistente Inteligente': MessageSquare,
      'Speech-to-Text': Mic,
      'Análise com GPT-4': Brain,
      'Summarization': FileText,
      'Geração de Acordos': FileText,
      'Síntese de Voz': Zap,
      'TTS': Zap,
      'Whisper': Mic,
      'GPT-4 Vision': Brain,
    };
    return iconMap[featureName] || LayoutDashboard;
  };

  return (
    <div className="min-h-screen bg-white dark:bg-gray-900">
      {/* Header */}
      <header className="sticky top-0 z-40 w-full border-b border-gray-200 dark:border-gray-800 bg-white/80 dark:bg-gray-900/80 backdrop-blur-sm">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="flex h-16 items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="h-10 w-10 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                <LayoutDashboard className="w-6 h-6 text-white" />
              </div>
              <span className="text-xl font-bold text-gray-900 dark:text-white">
                Assistente Executivo
              </span>
            </div>

            <nav className="hidden md:flex items-center gap-8">
              <Link
                href="#problem"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
              >
                {t('header.navProblem')}
              </Link>
              <Link
                href="#solution"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
              >
                {t('header.navSolution')}
              </Link>
              <div className="relative group">
                <button className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors flex items-center gap-1">
                  🚀 AI
                  <svg className="h-3 w-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
                  </svg>
                </button>
                <div className="absolute left-0 mt-0 w-48 rounded-lg shadow-lg bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-50">
                  <Link href="#solution" className="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-t-lg">
                    IA em cada pilar
                  </Link>
                  <Link href="#aiFeatures" className="block px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-blue-50 dark:hover:bg-blue-900/20 border-t border-gray-200 dark:border-gray-700 rounded-b-lg">
                    Tecnologias de IA
                  </Link>
                </div>
              </div>
              <Link
                href="#proof"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
              >
                {t('header.navProof')}
              </Link>
              <Link
                href="#pricing"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
              >
                {t('header.navPricing')}
              </Link>
              <Link
                href="/login"
                className="text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
              >
                {t('header.navLogin')}
              </Link>
            </nav>

            <div className="flex items-center gap-4">
              <ThemeSelector />
              <LanguageSelector />
              <Link
                href="/login"
                className="hidden md:inline-flex items-center px-4 py-2 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 text-white text-sm font-semibold hover:from-blue-600 hover:to-purple-700 transition-all shadow-md hover:shadow-lg"
              >
                {t('header.ctaStart')}
              </Link>
            </div>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <section className="relative overflow-hidden bg-gradient-to-br from-blue-50 via-white to-purple-50 dark:from-gray-900 dark:via-gray-900 dark:to-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-32 sm:py-40">
          <div className="mx-auto max-w-3xl text-center">
            <h1 className="text-5xl sm:text-6xl lg:text-7xl font-extrabold text-gray-900 dark:text-white mb-2 leading-tight">
              {t('hero.title')}
              <span className="block text-transparent bg-clip-text bg-gradient-to-r from-blue-600 to-purple-600">
                {t('hero.titleHighlight')}
              </span>
            </h1>
            <p className="mt-8 text-lg sm:text-xl text-gray-600 dark:text-gray-300 leading-relaxed max-w-2xl mx-auto">
              {t('hero.subtitle')}
            </p>

            <div className="mt-12 flex flex-col sm:flex-row items-center justify-center gap-4">
              <Link
                href="/login"
                className="inline-flex items-center px-8 py-4 rounded-lg bg-gradient-to-r from-blue-500 to-blue-600 text-white font-semibold hover:from-blue-600 hover:to-blue-700 transition-all shadow-lg hover:shadow-xl transform hover:scale-105"
              >
                {t('hero.ctaPrimary')}
              </Link>
              <button
                onClick={() => {
                  const email = "sales@assistenteexecutivo.com";
                  window.location.href = `mailto:${email}?subject=Agendar Demo`;
                }}
                className="inline-flex items-center px-8 py-4 rounded-lg border-2 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 font-semibold hover:border-blue-500 dark:hover:border-blue-500 hover:text-blue-600 dark:hover:text-blue-400 transition-all"
              >
                {t('hero.ctaSecondary')}
              </button>
            </div>

            <p className="mt-8 text-sm text-gray-600 dark:text-gray-400">
              {t('hero.proof')}
            </p>
          </div>
        </div>
      </section>

      {/* Problem Section */}
      <section id="problem" className="py-24 sm:py-32 bg-gray-50 dark:bg-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-20">
            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 dark:text-white mb-6">
              {t('problem.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300 leading-relaxed">
              {t('problem.subtitle')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-3">
            {problemItems.map((item, index) => (
              <div
                key={index}
                className="relative rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 p-8 hover:border-blue-300 dark:hover:border-blue-600 hover:shadow-lg transition-all"
              >
                <div className="mb-6 h-14 w-14 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                  <svg className="h-7 w-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={getIconSvg(item.icon)} />
                  </svg>
                </div>
                <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
                  {item.title}
                </h3>
                <p className="text-gray-600 dark:text-gray-300 leading-relaxed">
                  {item.description}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Solution Section */}
      <section id="solution" className="py-24 sm:py-32 bg-white dark:bg-gray-900">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-20">
            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 dark:text-white mb-6">
              {t('solution.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300 leading-relaxed">
              {t('solution.subtitle')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-8 lg:grid-cols-3">
            {solutionPillars.map((pillar, index) => (
              <div
                key={index}
                className="rounded-2xl border border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800 p-8 hover:shadow-lg transition-all"
              >
                <div className="mb-6 h-14 w-14 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                  <svg className="h-7 w-7 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={getIconSvg(pillar.icon)} />
                  </svg>
                </div>
                <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
                  {pillar.title}
                </h3>
                <p className="text-gray-600 dark:text-gray-300 mb-6 leading-relaxed">
                  {pillar.description}
                </p>
                <ul className="space-y-3">
                  {pillar.benefits.map((benefit: string, i: number) => (
                    <li key={i} className="flex gap-3 text-gray-700 dark:text-gray-300">
                      <span className="text-blue-500 font-bold">✓</span>
                      <span className="text-sm leading-relaxed">{benefit}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* AI Features Section */}
      <section className="py-24 sm:py-32 bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-20">
            <div className="inline-block mb-4 px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 text-xs font-semibold rounded-full">
              🚀 Powered by OpenAI & Advanced AI
            </div>
            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 dark:text-white mb-6">
              {t('aiFeatures.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300 leading-relaxed">
              {t('aiFeatures.subtitle')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
            {aiFeatures.map((feature, index) => {
              const IconComponent = getAIFeatureIcon(feature.name);
              return (
                <div
                  key={index}
                  className="rounded-2xl border border-gray-200 dark:border-gray-700 bg-gradient-to-br from-gray-50 to-blue-50 dark:from-gray-800 dark:to-gray-800/50 p-6 hover:border-blue-300 dark:hover:border-blue-600 hover:shadow-lg transition-all"
                >
                  <div className="mb-4 h-12 w-12 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                    <IconComponent className="w-6 h-6 text-white" />
                  </div>
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                    {feature.name}
                  </h3>
                  <p className="text-gray-600 dark:text-gray-300 text-sm leading-relaxed">
                    {feature.description}
                  </p>
                </div>
              );
            })}
          </div>

          <div className="mt-16 rounded-2xl border border-blue-200 dark:border-blue-800 bg-gradient-to-r from-blue-50 to-purple-50 dark:from-blue-900/20 dark:to-purple-900/20 p-8 text-center">
            <p className="text-sm font-semibold text-blue-600 dark:text-blue-400 mb-2">
              ✨ Tecnologia Avançada
            </p>
            <p className="text-lg text-gray-900 dark:text-white font-semibold">
              Cada recurso utiliza inteligência artificial para economizar seu tempo e aumentar sua produtividade.
            </p>
            <p className="text-sm text-gray-600 dark:text-gray-300 mt-4">
              GPT-4 Vision • Whisper • Function Calling • Custom Workflows • Real-time Processing
            </p>
          </div>
        </div>
      </section>

      {/* Proof Section */}
      <section id="proof" className="py-24 sm:py-32 bg-gray-50 dark:bg-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-20">
            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 dark:text-white mb-6">
              {t('proof.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300 leading-relaxed">
              {t('proof.subtitle')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
            {caseStudies.map((cs, index) => (
              <div
                key={index}
                className="rounded-2xl border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 p-8 hover:shadow-lg transition-all"
              >
                <div className="mb-6">
                  <p className="text-2xl font-bold text-blue-600 dark:text-blue-400 mb-2">
                    {cs.result}
                  </p>
                  <p className="text-sm font-semibold text-gray-600 dark:text-gray-400">
                    {cs.company} • {cs.role}
                  </p>
                </div>
                <p className="text-gray-700 dark:text-gray-300 italic mb-6 leading-relaxed">
                  "{cs.quote}"
                </p>
                <p className="text-sm font-semibold text-gray-900 dark:text-white">
                  — {cs.author}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Pricing Section */}
      <section id="pricing" className="py-24 sm:py-32 bg-white dark:bg-gray-900">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-2xl text-center mb-20">
            <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-gray-900 dark:text-white mb-6">
              {t('pricing.title')}
            </h2>
            <p className="text-lg text-gray-600 dark:text-gray-300 leading-relaxed">
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
              <div
                key={plan.name || index}
                className={`relative rounded-2xl overflow-hidden transition-all ${
                  plan.highlighted
                    ? 'border-2 border-blue-500 lg:scale-105 shadow-2xl'
                    : 'border border-gray-200 dark:border-gray-700'
                } ${
                  plan.highlighted ? 'bg-gradient-to-br from-blue-50 to-purple-50 dark:from-blue-900/10 dark:to-purple-900/10' : 'bg-white dark:bg-gray-800'
                }`}
              >
                {plan.highlighted && (
                  <div className="absolute top-0 left-0 right-0 bg-gradient-to-r from-blue-500 to-purple-600 text-white text-sm font-bold py-2 text-center">
                    ⭐ {plan.tagline}
                  </div>
                )}

                <div className="p-8 pt-12">
                  <h3 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
                    {plan.name}
                  </h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
                    {plan.description}
                  </p>

                  <div className="mb-8">
                    <p className="text-4xl font-extrabold text-gray-900 dark:text-white">
                      {plan.price}
                    </p>
                    {plan.price !== "Sob consulta" && (
                      <p className="text-sm text-gray-600 dark:text-gray-400 mt-2">
                        por mês, faturado anualmente
                      </p>
                    )}
                  </div>

                  <Link
                    href="/login"
                    className={`w-full inline-flex items-center justify-center px-6 py-3 rounded-lg font-semibold transition-all mb-8 ${
                      plan.highlighted
                        ? 'bg-gradient-to-r from-blue-500 to-purple-600 text-white hover:from-blue-600 hover:to-purple-700 shadow-lg hover:shadow-xl'
                        : 'border-2 border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:border-blue-500 dark:hover:border-blue-500 hover:text-blue-600 dark:hover:text-blue-400'
                    }`}
                  >
                    {t('pricing.selectPlan')}
                  </Link>

                  <div className="space-y-3 border-t border-gray-200 dark:border-gray-700 pt-8">
                    {plan.features.map((feature, i) => (
                      <div key={i} className="flex gap-3">
                        <span className="text-blue-500 font-bold flex-shrink-0">✓</span>
                        <span className="text-gray-700 dark:text-gray-300 text-sm leading-relaxed">
                          {feature}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Final CTA Section */}
      <section className="py-20 sm:py-24 bg-gradient-to-r from-blue-500 via-purple-500 to-purple-600">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl sm:text-4xl lg:text-5xl font-extrabold text-white mb-6">
            {t('cta.title')}
          </h2>
          <p className="text-lg text-blue-100 mb-10 max-w-2xl mx-auto">
            {t('cta.subtitle')}
          </p>
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4">
            <Link
              href="/login"
              className="inline-flex items-center px-8 py-4 rounded-lg bg-white text-blue-600 font-semibold hover:bg-gray-100 transition-all shadow-lg hover:shadow-xl"
            >
              {t('cta.button')}
            </Link>
            <button
              onClick={() => {
                const email = "sales@assistenteexecutivo.com";
                window.location.href = `mailto:${email}?subject=Agendar Demo`;
              }}
              className="inline-flex items-center px-8 py-4 rounded-lg border-2 border-white text-white font-semibold hover:bg-white/10 transition-all"
            >
              {t('cta.secondary')}
            </button>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="bg-gray-900 text-gray-300 border-t border-gray-800">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-12">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-4 mb-8">
            <div className="col-span-1 md:col-span-2">
              <div className="flex items-center gap-3 mb-4">
                <div className="h-10 w-10 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center">
                  <LayoutDashboard className="w-6 h-6 text-white" />
                </div>
                <span className="text-xl font-bold text-white">Assistente Executivo</span>
              </div>
              <p className="text-sm text-gray-400 max-w-md leading-relaxed">
                {t('footer.description')}
              </p>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-white mb-4">{t('footer.product')}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <Link href="#solution" className="text-gray-400 hover:text-blue-400 transition-colors">
                    {t('footer.navFeatures')}
                  </Link>
                </li>
                <li>
                  <Link href="#pricing" className="text-gray-400 hover:text-blue-400 transition-colors">
                    {t('footer.navPricing')}
                  </Link>
                </li>
                <li>
                  <Link href="/login" className="text-gray-400 hover:text-blue-400 transition-colors">
                    {t('footer.navLogin')}
                  </Link>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="text-sm font-semibold text-white mb-4">{t('footer.support')}</h3>
              <ul className="space-y-2 text-sm">
                <li>
                  <a href="#" className="text-gray-400 hover:text-blue-400 transition-colors">
                    {t('footer.documentation')}
                  </a>
                </li>
                <li>
                  <a href="#" className="text-gray-400 hover:text-blue-400 transition-colors">
                    {t('footer.contact')}
                  </a>
                </li>
                <li>
                  <a href="#" className="text-gray-400 hover:text-blue-400 transition-colors">
                    {t('footer.faq')}
                  </a>
                </li>
              </ul>
            </div>
          </div>

          <div className="pt-8 border-t border-gray-800 flex flex-col sm:flex-row justify-between items-center gap-4">
            <p className="text-sm text-gray-400">
              {t('footer.copyright', { year: new Date().getFullYear() })}
            </p>
            <div className="flex gap-6 text-sm">
              <a href="#" className="text-gray-400 hover:text-blue-400 transition-colors">
                {t('footer.terms')}
              </a>
              <a href="#" className="text-gray-400 hover:text-blue-400 transition-colors">
                {t('footer.privacy')}
              </a>
            </div>
          </div>
        </div>
      </footer>
    </div>
  );
}
