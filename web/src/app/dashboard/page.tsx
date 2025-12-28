'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useTranslations } from 'next-intl'
import { getCreditBalance, listCreditTransactions } from '@/lib/api/creditsApi'
import { listContactsClient } from '@/lib/api/contactsApiClient'
import { getBffSession } from '@/lib/bff'
import type { CreditBalance } from '@/lib/types/credit'
import type { Contact } from '@/lib/types/contact'
import { LayoutWrapper } from '@/components/LayoutWrapper'
import { StatsCard } from '@/components/StatsCard'
import { ContactCard } from '@/components/ContactCard'
import { BusinessCardScanner } from '@/components/BusinessCardScanner'
import { NetworkGraph } from '@/components/NetworkGraph'
import { RecentActivity } from '@/components/RecentActivity'
import { Users, CreditCard, Mic, Network } from 'lucide-react'

interface DashboardStats {
  totalContacts: number
  creditBalance: CreditBalance | null
  recentContacts: Contact[]
  scannedCards: number
  audioNotes: number
  connections: number
}

export default function DashboardPage() {
  const router = useRouter()
  const t = useTranslations('dashboard')
  const [loading, setLoading] = useState(true)
  const [stats, setStats] = useState<DashboardStats>({
    totalContacts: 0,
    creditBalance: null,
    recentContacts: [],
    scannedCards: 0,
    audioNotes: 0,
    connections: 0
  })
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let isMounted = true;
    
    async function loadDashboardData() {
      try {
        // Verificar autenticação
        console.log('[Dashboard] Verificando autenticação...');
        const session = await getBffSession();
        console.log('[Dashboard] Sessão recebida:', { 
          authenticated: session.authenticated, 
          hasUser: !!session.user,
          userEmail: session.user?.email 
        });
        
        if (!isMounted) return;
        
        if (!session.authenticated) {
          console.log('[Dashboard] Não autenticado, redirecionando para login');
          
          // Verificar se já estamos em um loop de redirect
          const redirectCount = parseInt(sessionStorage.getItem('redirectCount') || '0');
          
          if (redirectCount > 3) {
            console.error('[Dashboard] Loop de redirect detectado! Parando para evitar loop infinito.');
            setError('Erro de autenticação. Por favor, limpe os cookies e tente novamente.');
            sessionStorage.removeItem('redirectCount');
            return;
          }
          
          sessionStorage.setItem('redirectCount', (redirectCount + 1).toString());
          
          // Usar window.location.href ao invés de router.push para evitar loops
          const currentPath = window.location.pathname;
          window.location.href = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
          return;
        }
        
        // Se autenticado, limpar contador de redirects
        sessionStorage.removeItem('redirectCount');
        
        console.log('[Dashboard] Autenticado, carregando dados...');

        // Carregar dados em paralelo
        const [balanceResult, contactsResult] = await Promise.allSettled([
          getCreditBalance(),
          listContactsClient({ page: 1, pageSize: 4 }) // Para obter os 4 contatos recentes
        ])

        const newStats: DashboardStats = {
          totalContacts: 0,
          creditBalance: null,
          recentContacts: [],
          scannedCards: 0,
          audioNotes: 0,
          connections: 0
        }

        // Processar saldo
        if (balanceResult.status === 'fulfilled') {
          newStats.creditBalance = balanceResult.value
        }

        // Processar contatos
        if (contactsResult.status === 'fulfilled') {
          newStats.totalContacts = contactsResult.value.total
          newStats.recentContacts = contactsResult.value.contacts
          
          // Calcular conexões (soma de todos os relacionamentos)
          newStats.connections = contactsResult.value.contacts.reduce(
            (acc, contact) => acc + (contact.relationships?.length || 0),
            0
          )
        }

        // TODO: Carregar dados reais de cartões escaneados e notas de áudio
        // Por enquanto, valores mockados baseados nos contatos
        newStats.scannedCards = Math.floor(newStats.totalContacts * 0.36) // ~36% dos contatos
        newStats.audioNotes = Math.floor(newStats.totalContacts * 0.63) // ~63% dos contatos

        setStats(newStats)
      } catch (err) {
        console.error('Erro ao carregar dashboard:', err)
        setError(t('error'))
      } finally {
        setLoading(false)
      }
    }

    loadDashboardData();
    
    return () => {
      isMounted = false;
    };
  }, [router, t])

  if (loading) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
          <p className="text-muted-foreground">{t('loading')}</p>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="text-center">
          <p className="text-destructive mb-4">{error}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
          >
            Tentar novamente
          </button>
        </div>
      </div>
    )
  }

  // Calcular mudanças (mockado por enquanto)
  const contactsThisMonth = Math.floor(stats.totalContacts * 0.05) // ~5% do total
  const cardsThisWeek = Math.floor(stats.scannedCards * 0.06) // ~6% dos cartões
  const newConnections = Math.floor(stats.connections * 0.065) // ~6.5% das conexões

  return (
    <LayoutWrapper title="Dashboard" subtitle="Visão geral dos seus relacionamentos" activeTab="dashboard">
      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <StatsCard 
          title="Total de Contatos" 
          value={stats.totalContacts} 
          change={`+${contactsThisMonth} este mês`} 
          changeType="positive" 
          icon={Users}
          delay={0}
        />
        <StatsCard 
          title="Cartões Escaneados" 
          value={stats.scannedCards} 
          change={`+${cardsThisWeek} esta semana`} 
          changeType="positive" 
          icon={CreditCard}
          delay={50}
        />
        <StatsCard 
          title="Notas de Áudio" 
          value={stats.audioNotes} 
          change="3h gravadas" 
          changeType="neutral" 
          icon={Mic}
          delay={100}
        />
        <StatsCard 
          title="Conexões Mapeadas" 
          value={stats.connections} 
          change={`+${newConnections} novas`} 
          changeType="positive" 
          icon={Network}
          delay={150}
        />
      </div>
      
      {/* Main Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column */}
        <div className="lg:col-span-2 space-y-6">
          <NetworkGraph 
            height={400}
            maxDepth={2}
            showControls={false}
            showNodeDetails={false}
            showFullPageLink={true}
          />
          
          <div>
            <h2 className="font-semibold mb-4 text-lg">Contatos Recentes</h2>
            {stats.recentContacts.length > 0 ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {stats.recentContacts.map((contact, i) => (
                  <ContactCard key={contact.contactId} contact={contact} delay={i * 50} />
                ))}
              </div>
            ) : (
              <div className="glass-card p-8 text-center">
                <p className="text-muted-foreground">Nenhum contato encontrado. Comece adicionando seu primeiro contato!</p>
              </div>
            )}
          </div>
        </div>
        
        {/* Right Column */}
        <div className="space-y-6">
          <BusinessCardScanner />
          <RecentActivity />
        </div>
      </div>
    </LayoutWrapper>
  )
}
