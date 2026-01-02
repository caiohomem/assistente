"use client";

import { useEffect, useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import { getBffSession } from "@/lib/bff";
import { listContactsClient } from "@/lib/api/contactsApiClient";
import { LayoutWrapper } from "@/components/LayoutWrapper";
import {
  Building2,
  Users,
  Search,
  Loader2,
  Globe,
  Mail,
  ChevronRight,
} from "lucide-react";
import { cn } from "@/lib/utils";
import type { Contact } from "@/lib/types/contact";

interface CompanyInfo {
  name: string;
  contacts: Contact[];
  domains: string[];
}

export default function EmpresasPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function load() {
      try {
        const session = await getBffSession();
        if (!session.authenticated) {
          window.location.href = `/login?returnUrl=${encodeURIComponent("/empresas")}`;
          return;
        }

        // Load all contacts to extract companies
        const data = await listContactsClient({ page: 1, pageSize: 1000 });
        if (!isMounted) return;
        setContacts(data.contacts);
      } catch (e) {
        console.error("Error loading contacts:", e);
        if (!isMounted) return;
        setError(e instanceof Error ? e.message : "Erro ao carregar dados");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    load();
    return () => {
      isMounted = false;
    };
  }, []);

  // Extract companies from contacts
  const companies = useMemo(() => {
    const companyMap = new Map<string, CompanyInfo>();

    contacts.forEach((contact) => {
      if (contact.company && contact.company.trim()) {
        const companyName = contact.company.trim();
        const normalizedName = companyName.toLowerCase();

        if (!companyMap.has(normalizedName)) {
          companyMap.set(normalizedName, {
            name: companyName,
            contacts: [],
            domains: [],
          });
        }

        const company = companyMap.get(normalizedName)!;
        company.contacts.push(contact);

        // Extract domains from emails
        contact.emails.forEach((email) => {
          const domain = email.split("@")[1];
          if (domain && !company.domains.includes(domain)) {
            company.domains.push(domain);
          }
        });
      }
    });

    return Array.from(companyMap.values()).sort((a, b) =>
      a.name.localeCompare(b.name, "pt-BR")
    );
  }, [contacts]);

  // Filter companies by search term
  const filteredCompanies = useMemo(() => {
    if (!searchTerm.trim()) return companies;
    const term = searchTerm.toLowerCase();
    return companies.filter(
      (company) =>
        company.name.toLowerCase().includes(term) ||
        company.domains.some((d) => d.toLowerCase().includes(term))
    );
  }, [companies, searchTerm]);

  if (loading) {
    return (
      <LayoutWrapper
        title="Empresas"
        subtitle="Organizações dos seus contatos"
        activeTab="companies"
      >
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-6 h-6 animate-spin text-primary mr-2" />
          <span className="text-muted-foreground">Carregando...</span>
        </div>
      </LayoutWrapper>
    );
  }

  return (
    <LayoutWrapper
      title="Empresas"
      subtitle="Organizações dos seus contatos"
      activeTab="companies"
    >
      <div className="space-y-6">
        {/* Error message */}
        {error && (
          <div className="glass-card p-4 bg-destructive/10 border-destructive/30">
            <span className="text-destructive text-sm">{error}</span>
          </div>
        )}

        {/* Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="glass-card p-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-primary/20 flex items-center justify-center">
                <Building2 className="w-5 h-5 text-primary" />
              </div>
              <div>
                <p className="text-2xl font-bold">{companies.length}</p>
                <p className="text-xs text-muted-foreground">Empresas</p>
              </div>
            </div>
          </div>
          <div className="glass-card p-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-accent/20 flex items-center justify-center">
                <Users className="w-5 h-5 text-accent" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {contacts.filter((c) => c.company).length}
                </p>
                <p className="text-xs text-muted-foreground">Contatos vinculados</p>
              </div>
            </div>
          </div>
          <div className="glass-card p-4">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-green-500/20 flex items-center justify-center">
                <Globe className="w-5 h-5 text-green-500" />
              </div>
              <div>
                <p className="text-2xl font-bold">
                  {companies.reduce((acc, c) => acc + c.domains.length, 0)}
                </p>
                <p className="text-xs text-muted-foreground">Domínios identificados</p>
              </div>
            </div>
          </div>
        </div>

        {/* Search */}
        <div className="glass-card p-4">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar empresa ou domínio..."
              className="w-full pl-10 pr-4 py-2 bg-secondary/50 border border-border rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50"
            />
          </div>
        </div>

        {/* Companies list */}
        {filteredCompanies.length === 0 ? (
          <div className="glass-card p-8 text-center">
            <Building2 className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
            <h3 className="text-lg font-semibold mb-2">
              {searchTerm
                ? "Nenhuma empresa encontrada"
                : "Nenhuma empresa cadastrada"}
            </h3>
            <p className="text-sm text-muted-foreground">
              {searchTerm
                ? "Tente buscar por outro termo."
                : "Adicione o campo empresa nos seus contatos para visualizá-las aqui."}
            </p>
          </div>
        ) : (
          <div className="grid gap-4">
            {filteredCompanies.map((company) => (
              <div
                key={company.name}
                className="glass-card-hover p-4 cursor-pointer"
                onClick={() =>
                  router.push(
                    `/contatos?empresa=${encodeURIComponent(company.name)}`
                  )
                }
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-4">
                    <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-primary/20 to-accent/20 flex items-center justify-center flex-shrink-0">
                      <Building2 className="w-6 h-6 text-primary" />
                    </div>
                    <div className="min-w-0">
                      <h3 className="font-semibold text-lg">{company.name}</h3>
                      <div className="flex flex-wrap items-center gap-3 mt-1">
                        <span className="flex items-center gap-1 text-sm text-muted-foreground">
                          <Users className="w-3 h-3" />
                          {company.contacts.length} contato
                          {company.contacts.length !== 1 ? "s" : ""}
                        </span>
                        {company.domains.length > 0 && (
                          <span className="flex items-center gap-1 text-sm text-muted-foreground">
                            <Mail className="w-3 h-3" />
                            {company.domains.slice(0, 2).join(", ")}
                            {company.domains.length > 2 &&
                              ` +${company.domains.length - 2}`}
                          </span>
                        )}
                      </div>
                      {/* Contact avatars */}
                      <div className="flex items-center gap-1 mt-2">
                        {company.contacts.slice(0, 5).map((contact, idx) => (
                          <div
                            key={contact.contactId}
                            className={cn(
                              "w-7 h-7 rounded-full bg-secondary flex items-center justify-center text-xs font-medium border-2 border-card",
                              idx > 0 && "-ml-2"
                            )}
                            title={`${contact.firstName} ${contact.lastName || ""}`}
                          >
                            {contact.firstName[0]}
                            {contact.lastName?.[0] || ""}
                          </div>
                        ))}
                        {company.contacts.length > 5 && (
                          <div className="w-7 h-7 rounded-full bg-secondary/50 flex items-center justify-center text-xs text-muted-foreground -ml-2 border-2 border-card">
                            +{company.contacts.length - 5}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                  <ChevronRight className="w-5 h-5 text-muted-foreground flex-shrink-0" />
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Info banner */}
        <div className="glass-card p-4 bg-primary/5 border-primary/20">
          <div className="flex items-start gap-3">
            <Building2 className="w-5 h-5 text-primary flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-primary">
                Empresas extraídas automaticamente
              </p>
              <p className="text-xs text-muted-foreground mt-1">
                Esta lista é gerada a partir do campo &quot;Empresa&quot; dos seus contatos.
                Em breve você poderá adicionar informações detalhadas como CNPJ,
                endereço e notas para cada empresa.
              </p>
            </div>
          </div>
        </div>
      </div>
    </LayoutWrapper>
  );
}
