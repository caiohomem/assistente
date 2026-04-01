"use client";

import { useState, useMemo } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Contact } from "@/lib/types/contact";
import { listContactsClient, searchContactsClient, ListContactsParams, SearchContactsParams, ListContactsResult } from "@/lib/api/contactsApiClient";
import { ContactCard } from "@/components/ContactCard";
import { Button } from "@/components/ui/button";
import { Search, Building2, X, ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

interface ContactsListClientProps {
  initialData: ListContactsResult;
  initialCompanyFilter?: string;
}

export function ContactsListClient({ initialData, initialCompanyFilter }: ContactsListClientProps) {
  const router = useRouter();
  const [allContacts, setAllContacts] = useState<Contact[]>(initialData.contacts);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [companyFilter, setCompanyFilter] = useState(initialCompanyFilter || "");
  const [showCompanyDropdown, setShowCompanyDropdown] = useState(false);
  const [currentPage, setCurrentPage] = useState(initialData.page);
  const [totalPages, setTotalPages] = useState(initialData.totalPages);
  const [total, setTotal] = useState(initialData.total);
  const [pageSize] = useState(initialData.pageSize);

  // Extract unique companies from contacts
  const availableCompanies = useMemo(() => {
    const companies = new Set<string>();
    allContacts.forEach((contact) => {
      if (contact.company && contact.company.trim()) {
        companies.add(contact.company.trim());
      }
    });
    return Array.from(companies).sort((a, b) => a.localeCompare(b, "pt-BR"));
  }, [allContacts]);

  // Filter contacts by company (client-side filtering)
  const contacts = useMemo(() => {
    if (!companyFilter) return allContacts;
    return allContacts.filter(
      (c) => c.company && c.company.toLowerCase() === companyFilter.toLowerCase()
    );
  }, [allContacts, companyFilter]);

  const loadContacts = async (page: number, search?: string) => {
    setLoading(true);
    try {
      let result: ListContactsResult;
      if (search && search.trim()) {
        const params: SearchContactsParams = {
          searchTerm: search.trim(),
          page,
          pageSize,
        };
        result = await searchContactsClient(params);
      } else {
        const params: ListContactsParams = {
          page,
          pageSize,
        };
        result = await listContactsClient(params);
      }
      setAllContacts(result.contacts);
      setCurrentPage(result.page);
      setTotalPages(result.totalPages);
      setTotal(result.total);
    } catch (error) {
      console.error("Erro ao carregar contatos:", error);
    } finally {
      setLoading(false);
    }
  };

  // Update URL when company filter changes
  const handleCompanyFilterChange = (company: string) => {
    setCompanyFilter(company);
    setShowCompanyDropdown(false);
    // Update URL
    const params = new URLSearchParams(window.location.search);
    if (company) {
      params.set("empresa", company);
    } else {
      params.delete("empresa");
    }
    const newUrl = params.toString() ? `/contatos?${params.toString()}` : "/contatos";
    router.push(newUrl, { scroll: false });
  };

  const clearCompanyFilter = () => {
    handleCompanyFilterChange("");
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
    loadContacts(1, searchTerm);
  };

  const handlePageChange = (newPage: number) => {
    if (newPage >= 1 && newPage <= totalPages) {
      loadContacts(newPage, searchTerm || undefined);
    }
  };

  // Count filtered results
  const filteredTotal = contacts.length;

  return (
    <>
      {/* Info */}
      <div className="mb-6 flex items-center justify-between">
        <p className="text-sm text-muted-foreground">
          {filteredTotal} {filteredTotal === 1 ? "contato encontrado" : "contatos encontrados"}
          {companyFilter && (
            <span className="ml-1">
              em <span className="font-medium text-foreground">{companyFilter}</span>
            </span>
          )}
        </p>
        {companyFilter && (
          <Button
            variant="ghost"
            size="sm"
            onClick={clearCompanyFilter}
            className="text-xs"
          >
            <X className="w-3 h-3 mr-1" />
            Limpar filtro
          </Button>
        )}
      </div>

      {/* Search and Filters */}
      <div className="mb-6 space-y-3">
        {/* Search */}
        <form onSubmit={handleSearch}>
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Buscar por nome, email, telefone..."
                className="w-full rounded-xl border border-border bg-secondary/50 backdrop-blur-sm pl-11 pr-4 py-3 text-sm transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50 focus:bg-secondary/80 placeholder:text-muted-foreground/70"
              />
            </div>
            <Button
              type="submit"
              disabled={loading}
              variant="glow"
            >
              {loading ? "Buscando..." : "Buscar"}
            </Button>
            {searchTerm && (
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setSearchTerm("");
                  setCurrentPage(1);
                  loadContacts(1);
                }}
              >
                Limpar
              </Button>
            )}
          </div>
        </form>

        {/* Company Filter */}
        {availableCompanies.length > 0 && (
          <div className="relative">
            <button
              type="button"
              onClick={() => setShowCompanyDropdown(!showCompanyDropdown)}
              className={cn(
                "flex items-center gap-2 px-4 py-2 rounded-xl border text-sm transition-all duration-200",
                companyFilter
                  ? "bg-primary/10 border-primary/30 text-primary"
                  : "bg-secondary/50 border-border text-muted-foreground hover:text-foreground hover:border-primary/30"
              )}
            >
              <Building2 className="w-4 h-4" />
              {companyFilter || "Filtrar por empresa"}
              <ChevronDown className={cn(
                "w-4 h-4 transition-transform",
                showCompanyDropdown && "rotate-180"
              )} />
            </button>

            {showCompanyDropdown && (
              <>
                {/* Backdrop */}
                <div
                  className="fixed inset-0 z-40"
                  onClick={() => setShowCompanyDropdown(false)}
                />
                {/* Dropdown */}
                <div className="absolute top-full left-0 mt-2 w-64 max-h-60 overflow-auto z-50 rounded-xl border border-border bg-card shadow-lg">
                  <button
                    type="button"
                    onClick={() => handleCompanyFilterChange("")}
                    className={cn(
                      "w-full px-4 py-2 text-left text-sm hover:bg-secondary/50 transition-colors",
                      !companyFilter && "bg-primary/10 text-primary"
                    )}
                  >
                    Todas as empresas
                  </button>
                  {availableCompanies.map((company) => (
                    <button
                      key={company}
                      type="button"
                      onClick={() => handleCompanyFilterChange(company)}
                      className={cn(
                        "w-full px-4 py-2 text-left text-sm hover:bg-secondary/50 transition-colors truncate",
                        companyFilter === company && "bg-primary/10 text-primary"
                      )}
                    >
                      {company}
                    </button>
                  ))}
                </div>
              </>
            )}
          </div>
        )}
      </div>

      {/* Contacts Grid */}
      {loading && contacts.length === 0 ? (
        <div className="glass-card p-12 text-center">
          <p className="text-muted-foreground">Carregando contatos...</p>
        </div>
      ) : contacts.length === 0 ? (
        <div className="glass-card p-12 text-center">
          <p className="text-muted-foreground">
            {searchTerm ? "Nenhum contato encontrado com os filtros aplicados." : "Nenhum contato cadastrado ainda."}
          </p>
          {!searchTerm && (
            <Link
              href="/contatos?novo=true"
              className="mt-4 inline-block text-sm font-medium text-primary hover:underline"
            >
              Criar primeiro contato
            </Link>
          )}
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
            {contacts.map((contact, i) => (
              <ContactCard key={contact.contactId} contact={contact} delay={i * 50} />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="mt-6 flex items-center justify-between border-t border-border glass-card px-4 py-3 sm:px-6">
              <div className="flex flex-1 justify-between sm:hidden">
                <Button
                  variant="ghost"
                  onClick={() => handlePageChange(currentPage - 1)}
                  disabled={currentPage === 1 || loading}
                >
                  Anterior
                </Button>
                <Button
                  variant="ghost"
                  onClick={() => handlePageChange(currentPage + 1)}
                  disabled={currentPage === totalPages || loading}
                >
                  Próxima
                </Button>
              </div>
              <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">
                    Mostrando <span className="font-medium text-foreground">{(currentPage - 1) * pageSize + 1}</span> até{" "}
                    <span className="font-medium text-foreground">
                      {Math.min(currentPage * pageSize, total)}
                    </span>{" "}
                    de <span className="font-medium text-foreground">{total}</span> resultados
                  </p>
                </div>
                <div>
                  <nav className="isolate inline-flex -space-x-px rounded-xl" aria-label="Pagination">
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handlePageChange(currentPage - 1)}
                      disabled={currentPage === 1 || loading}
                      className="rounded-r-none"
                    >
                      <span className="sr-only">Anterior</span>
                      <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path
                          fillRule="evenodd"
                          d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z"
                          clipRule="evenodd"
                        />
                      </svg>
                    </Button>
                    {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                      let pageNum: number;
                      if (totalPages <= 5) {
                        pageNum = i + 1;
                      } else if (currentPage <= 3) {
                        pageNum = i + 1;
                      } else if (currentPage >= totalPages - 2) {
                        pageNum = totalPages - 4 + i;
                      } else {
                        pageNum = currentPage - 2 + i;
                      }
                      return (
                        <Button
                          key={pageNum}
                          variant={pageNum === currentPage ? "default" : "ghost"}
                          onClick={() => handlePageChange(pageNum)}
                          disabled={loading}
                          className="rounded-none"
                        >
                          {pageNum}
                        </Button>
                      );
                    })}
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handlePageChange(currentPage + 1)}
                      disabled={currentPage === totalPages || loading}
                      className="rounded-l-none"
                    >
                      <span className="sr-only">Próxima</span>
                      <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                        <path
                          fillRule="evenodd"
                          d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
                          clipRule="evenodd"
                        />
                      </svg>
                    </Button>
                  </nav>
                </div>
              </div>
            </div>
          )}
        </>
      )}
    </>
  );
}
