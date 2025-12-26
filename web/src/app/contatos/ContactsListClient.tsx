"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import { Contact } from "@/lib/types/contact";
import { listContactsClient, searchContactsClient, ListContactsParams, SearchContactsParams, ListContactsResult } from "@/lib/api/contactsApiClient";
import { TopBar } from "@/components/TopBar";

interface ContactsListClientProps {
  initialData: ListContactsResult;
}

export function ContactsListClient({ initialData }: ContactsListClientProps) {
  const [contacts, setContacts] = useState<Contact[]>(initialData.contacts);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(initialData.page);
  const [totalPages, setTotalPages] = useState(initialData.totalPages);
  const [total, setTotal] = useState(initialData.total);
  const [pageSize] = useState(initialData.pageSize);

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
      setContacts(result.contacts);
      setCurrentPage(result.page);
      setTotalPages(result.totalPages);
      setTotal(result.total);
    } catch (error) {
      console.error("Erro ao carregar contatos:", error);
      // TODO: Mostrar mensagem de erro ao usuário
    } finally {
      setLoading(false);
    }
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

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
    });
  };

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Contatos" showBackButton backHref="/dashboard">
        <Link
          href="/contatos/novo"
          className="inline-flex items-center justify-center rounded-md bg-zinc-900 dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700 focus:outline-none focus:ring-2 focus:ring-zinc-500 focus:ring-offset-2"
        >
          Novo Contato
        </Link>
      </TopBar>
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {/* Info */}
        <div className="mb-6">
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            {total} {total === 1 ? "contato encontrado" : "contatos encontrados"}
          </p>
        </div>

        {/* Search */}
        <form onSubmit={handleSearch} className="mb-6">
          <div className="flex gap-2">
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder="Buscar por nome, email, telefone, empresa..."
              className="flex-1 rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus:border-zinc-500 dark:focus:border-zinc-500 focus:outline-none focus:ring-2 focus:ring-zinc-500"
            />
            <button
              type="submit"
              disabled={loading}
              className="rounded-md bg-zinc-900 dark:bg-zinc-800 px-6 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:hover:bg-zinc-700 focus:outline-none focus:ring-2 focus:ring-zinc-500 focus:ring-offset-2 disabled:opacity-50"
            >
              {loading ? "Buscando..." : "Buscar"}
            </button>
            {searchTerm && (
              <button
                type="button"
                onClick={() => {
                  setSearchTerm("");
                  setCurrentPage(1);
                  loadContacts(1);
                }}
                className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 focus:outline-none focus:ring-2 focus:ring-zinc-500 focus:ring-offset-2"
              >
                Limpar
              </button>
            )}
          </div>
        </form>

        {/* Contacts List */}
        {loading && contacts.length === 0 ? (
          <div className="rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 p-12 text-center">
            <p className="text-zinc-600 dark:text-zinc-400">Carregando contatos...</p>
          </div>
        ) : contacts.length === 0 ? (
          <div className="rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 p-12 text-center">
            <p className="text-zinc-600 dark:text-zinc-400">
              {searchTerm ? "Nenhum contato encontrado com os filtros aplicados." : "Nenhum contato cadastrado ainda."}
            </p>
            {!searchTerm && (
              <Link
                href="/contatos/novo"
                className="mt-4 inline-block text-sm font-medium text-zinc-900 dark:text-zinc-100 hover:underline"
              >
                Criar primeiro contato
              </Link>
            )}
          </div>
        ) : (
          <>
            <div className="overflow-hidden rounded-md border border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 shadow-sm">
              <ul className="divide-y divide-zinc-200 dark:divide-zinc-700">
                {contacts.map((contact) => (
                  <li key={contact.contactId}>
                    <Link
                      href={`/contatos/${contact.contactId}`}
                      className="block px-4 py-4 hover:bg-zinc-50 dark:hover:bg-zinc-700 transition-colors"
                    >
                      <div className="flex items-center justify-between">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center gap-3">
                            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-zinc-200 dark:bg-zinc-700 text-sm font-medium text-zinc-700 dark:text-zinc-300">
                              {contact.firstName.charAt(0).toUpperCase()}
                              {contact.lastName?.charAt(0).toUpperCase() || ""}
                            </div>
                            <div className="flex-1 min-w-0">
                              <p className="text-sm font-medium text-zinc-900 dark:text-zinc-100 truncate">
                                {contact.fullName}
                              </p>
                              <div className="mt-1 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-zinc-500 dark:text-zinc-400">
                                {contact.company && (
                                  <span className="truncate">{contact.company}</span>
                                )}
                                {contact.jobTitle && (
                                  <span className="truncate">{contact.jobTitle}</span>
                                )}
                                {contact.emails.length > 0 && (
                                  <span className="truncate">{contact.emails[0]}</span>
                                )}
                                {contact.phones.length > 0 && (
                                  <span className="truncate">{contact.phones[0]}</span>
                                )}
                              </div>
                            </div>
                          </div>
                        </div>
                        <div className="ml-4 flex-shrink-0 text-xs text-zinc-500 dark:text-zinc-400">
                          {formatDate(contact.createdAt)}
                        </div>
                      </div>
                    </Link>
                  </li>
                ))}
              </ul>
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="mt-6 flex items-center justify-between border-t border-zinc-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-3 sm:px-6">
                <div className="flex flex-1 justify-between sm:hidden">
                  <button
                    onClick={() => handlePageChange(currentPage - 1)}
                    disabled={currentPage === 1 || loading}
                    className="relative inline-flex items-center rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Anterior
                  </button>
                  <button
                    onClick={() => handlePageChange(currentPage + 1)}
                    disabled={currentPage === totalPages || loading}
                    className="relative ml-3 inline-flex items-center rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Próxima
                  </button>
                </div>
                <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
                  <div>
                    <p className="text-sm text-zinc-700 dark:text-zinc-300">
                      Mostrando <span className="font-medium">{(currentPage - 1) * pageSize + 1}</span> até{" "}
                      <span className="font-medium">
                        {Math.min(currentPage * pageSize, total)}
                      </span>{" "}
                      de <span className="font-medium">{total}</span> resultados
                    </p>
                  </div>
                  <div>
                    <nav className="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                      <button
                        onClick={() => handlePageChange(currentPage - 1)}
                        disabled={currentPage === 1 || loading}
                        className="relative inline-flex items-center rounded-l-md px-2 py-2 text-zinc-400 dark:text-zinc-500 ring-1 ring-inset ring-zinc-300 dark:ring-zinc-700 hover:bg-zinc-50 dark:hover:bg-zinc-700 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        <span className="sr-only">Anterior</span>
                        <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                          <path
                            fillRule="evenodd"
                            d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z"
                            clipRule="evenodd"
                          />
                        </svg>
                      </button>
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
                          <button
                            key={pageNum}
                            onClick={() => handlePageChange(pageNum)}
                            disabled={loading}
                            className={`relative inline-flex items-center px-4 py-2 text-sm font-semibold ${
                              pageNum === currentPage
                                ? "z-10 bg-zinc-900 dark:bg-zinc-700 text-white focus:z-20 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-zinc-600 dark:focus-visible:outline-zinc-500"
                                : "text-zinc-900 dark:text-zinc-300 ring-1 ring-inset ring-zinc-300 dark:ring-zinc-700 hover:bg-zinc-50 dark:hover:bg-zinc-700 focus:z-20 focus:outline-offset-0"
                            } disabled:opacity-50 disabled:cursor-not-allowed`}
                          >
                            {pageNum}
                          </button>
                        );
                      })}
                      <button
                        onClick={() => handlePageChange(currentPage + 1)}
                        disabled={currentPage === totalPages || loading}
                        className="relative inline-flex items-center rounded-r-md px-2 py-2 text-zinc-400 dark:text-zinc-500 ring-1 ring-inset ring-zinc-300 dark:ring-zinc-700 hover:bg-zinc-50 dark:hover:bg-zinc-700 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                      >
                        <span className="sr-only">Próxima</span>
                        <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                          <path
                            fillRule="evenodd"
                            d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z"
                            clipRule="evenodd"
                          />
                        </svg>
                      </button>
                    </nav>
                  </div>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
