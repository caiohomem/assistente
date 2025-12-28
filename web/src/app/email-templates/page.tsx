"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { TopBar } from "@/components/TopBar";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import {
  listEmailTemplatesClient,
  deleteEmailTemplateClient,
  activateEmailTemplateClient,
  deactivateEmailTemplateClient,
  type EmailTemplate,
  type ListEmailTemplatesResult,
} from "@/lib/api/emailTemplatesApiClient";
import { EmailTemplateType } from "@/lib/types/emailTemplates";

export default function EmailTemplatesPage() {
  const router = useRouter();
  const [loading, setLoading] = useState(true);
  const [templates, setTemplates] = useState<EmailTemplate[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; templateId: string | null; templateName: string }>({
    isOpen: false,
    templateId: null,
    templateName: "",
  });
  const [deleting, setDeleting] = useState(false);
  const [filterActiveOnly, setFilterActiveOnly] = useState<boolean | undefined>(undefined);
  const [filterType, setFilterType] = useState<EmailTemplateType | undefined>(undefined);

  useEffect(() => {
    loadTemplates();
  }, [page, filterActiveOnly, filterType]);

  const loadTemplates = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await listEmailTemplatesClient({
        page,
        pageSize,
        activeOnly: filterActiveOnly,
        templateType: filterType,
      });
      setTemplates(result.templates);
      setTotal(result.total);
      setPage(result.page);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Erro ao carregar templates de email:", err);
      setError(err instanceof Error ? err.message : "Erro ao carregar templates de email");
    } finally {
      setLoading(false);
    }
  };

  const getTypeLabel = (type: EmailTemplateType): string => {
    switch (type) {
      case EmailTemplateType.UserCreated:
        return "Usuário Criado";
      case EmailTemplateType.PasswordReset:
        return "Redefinição de Senha";
      case EmailTemplateType.Welcome:
        return "Bem-vindo";
      default:
        return "Desconhecido";
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("pt-BR", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const handleDeleteClick = (templateId: string, templateName: string) => {
    setDeleteDialog({
      isOpen: true,
      templateId,
      templateName,
    });
  };

  const handleDeleteConfirm = async () => {
    if (!deleteDialog.templateId) return;

    setDeleting(true);
    try {
      await deleteEmailTemplateClient(deleteDialog.templateId);
      setDeleteDialog({ isOpen: false, templateId: null, templateName: "" });
      await loadTemplates();
      router.refresh();
    } catch (err) {
      console.error("Erro ao deletar template:", err);
      setError(err instanceof Error ? err.message : "Erro ao deletar template");
    } finally {
      setDeleting(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialog({ isOpen: false, templateId: null, templateName: "" });
  };

  const handleToggleActive = async (template: EmailTemplate) => {
    try {
      if (template.isActive) {
        await deactivateEmailTemplateClient(template.id);
      } else {
        await activateEmailTemplateClient(template.id);
      }
      await loadTemplates();
      router.refresh();
    } catch (err) {
      console.error("Erro ao alterar status do template:", err);
      setError(err instanceof Error ? err.message : "Erro ao alterar status do template");
    }
  };

  if (loading && templates.length === 0) {
    return (
      <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
        <TopBar title="Templates de Email" showBackButton backHref="/dashboard" />
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600 dark:border-indigo-400 mx-auto mb-4"></div>
            <p className="text-gray-600 dark:text-gray-300">Carregando templates...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-900">
      <TopBar title="Templates de Email" showBackButton backHref="/dashboard">
        <Link
          href="/email-templates/novo"
          className="inline-flex items-center justify-center rounded-md bg-indigo-600 dark:bg-indigo-500 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 dark:hover:bg-indigo-600 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          Novo Template
        </Link>
      </TopBar>
      <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {error && (
          <div className="mb-4 rounded-md bg-red-50 dark:bg-red-900/20 p-4">
            <p className="text-sm text-red-800 dark:text-red-200">{error}</p>
          </div>
        )}

        {/* Filtros */}
        <div className="mb-6 flex flex-wrap gap-4 items-center">
          <div className="flex items-center gap-2">
            <label className="text-sm text-zinc-600 dark:text-zinc-400">Filtrar:</label>
            <select
              value={filterActiveOnly === undefined ? "all" : filterActiveOnly ? "active" : "inactive"}
              onChange={(e) => {
                const value = e.target.value;
                setFilterActiveOnly(value === "all" ? undefined : value === "active");
              }}
              className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-3 py-1 text-sm text-zinc-700 dark:text-zinc-300"
            >
              <option value="all">Todos</option>
              <option value="active">Ativos</option>
              <option value="inactive">Inativos</option>
            </select>
          </div>
          <div className="flex items-center gap-2">
            <label className="text-sm text-zinc-600 dark:text-zinc-400">Tipo:</label>
            <select
              value={filterType === undefined ? "all" : filterType.toString()}
              onChange={(e) => {
                const value = e.target.value;
                setFilterType(value === "all" ? undefined : parseInt(value) as EmailTemplateType);
              }}
              className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-3 py-1 text-sm text-zinc-700 dark:text-zinc-300"
            >
              <option value="all">Todos</option>
              <option value={EmailTemplateType.UserCreated}>Usuário Criado</option>
              <option value={EmailTemplateType.PasswordReset}>Redefinição de Senha</option>
              <option value={EmailTemplateType.Welcome}>Bem-vindo</option>
            </select>
          </div>
          <div className="flex-1"></div>
          <p className="text-sm text-zinc-600 dark:text-zinc-400">
            {total} {total === 1 ? "template encontrado" : "templates encontrados"}
          </p>
        </div>

        {templates.length === 0 ? (
          <div className="rounded-lg bg-white dark:bg-zinc-800 p-8 text-center shadow">
            <p className="text-zinc-600 dark:text-zinc-400">Nenhum template encontrado.</p>
            <Link
              href="/email-templates/novo"
              className="mt-4 inline-block text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300"
            >
              Criar primeiro template
            </Link>
          </div>
        ) : (
          <>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {templates.map((template) => (
                <div
                  key={template.id}
                  className="rounded-lg bg-white dark:bg-zinc-800 p-6 shadow hover:shadow-md transition-shadow"
                >
                  <Link
                    href={`/email-templates/${template.id}`}
                    className="block"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100">
                        {template.name}
                      </h3>
                      {template.isActive ? (
                        <span className="inline-flex items-center rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 px-2.5 py-0.5 text-xs font-medium">
                          Ativo
                        </span>
                      ) : (
                        <span className="inline-flex items-center rounded-full bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200 px-2.5 py-0.5 text-xs font-medium">
                          Inativo
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-zinc-500 dark:text-zinc-400 mb-2">
                      Tipo: {getTypeLabel(template.templateType)}
                    </p>
                    <p className="text-sm text-zinc-600 dark:text-zinc-400 mb-2 line-clamp-2">
                      <strong>Assunto:</strong> {template.subject}
                    </p>
                    {template.placeholders.length > 0 && (
                      <p className="text-xs text-zinc-500 dark:text-zinc-400 mb-2">
                        Placeholders: {template.placeholders.join(", ")}
                      </p>
                    )}
                    <p className="text-xs text-zinc-500 dark:text-zinc-400">
                      Criado em: {formatDate(template.createdAt)}
                    </p>
                  </Link>
                  <div className="mt-4 flex gap-2">
                    <Link
                      href={`/email-templates/${template.id}/editar`}
                      className="flex-1 text-center rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-3 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700"
                    >
                      Editar
                    </Link>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        handleToggleActive(template);
                      }}
                      className={`flex-1 rounded-md border px-3 py-2 text-sm font-medium ${
                        template.isActive
                          ? "border-yellow-300 dark:border-yellow-700 bg-white dark:bg-zinc-800 text-yellow-700 dark:text-yellow-400 hover:bg-yellow-50 dark:hover:bg-yellow-900/20"
                          : "border-green-300 dark:border-green-700 bg-white dark:bg-zinc-800 text-green-700 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/20"
                      }`}
                    >
                      {template.isActive ? "Desativar" : "Ativar"}
                    </button>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        handleDeleteClick(template.id, template.name);
                      }}
                      className="flex-1 rounded-md border border-red-300 dark:border-red-700 bg-white dark:bg-zinc-800 px-3 py-2 text-sm font-medium text-red-700 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20"
                    >
                      Excluir
                    </button>
                  </div>
                </div>
              ))}
            </div>
            <ConfirmDialog
              isOpen={deleteDialog.isOpen}
              title="Excluir Template de Email"
              message={`Tem certeza que deseja excluir o template "${deleteDialog.templateName}"? Esta ação não pode ser desfeita.`}
              confirmText="Excluir"
              cancelText="Cancelar"
              onConfirm={handleDeleteConfirm}
              onCancel={handleDeleteCancel}
              isLoading={deleting}
              variant="danger"
            />

            {totalPages > 1 && (
              <div className="mt-6 flex items-center justify-between">
                <button
                  onClick={() => setPage(Math.max(1, page - 1))}
                  disabled={page === 1}
                  className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Anterior
                </button>
                <span className="text-sm text-zinc-600 dark:text-zinc-400">
                  Página {page} de {totalPages}
                </span>
                <button
                  onClick={() => setPage(Math.min(totalPages, page + 1))}
                  disabled={page === totalPages}
                  className="rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 px-4 py-2 text-sm font-medium text-zinc-700 dark:text-zinc-300 hover:bg-zinc-50 dark:hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Próxima
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

