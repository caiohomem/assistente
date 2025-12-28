"use client";

import { useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { listTemplatesClient, deleteTemplateClient, type Template, type ListTemplatesResult } from "@/lib/api/automationApiClient";
import { TemplateType } from "@/lib/types/automation";
import { NovoTemplateClient } from "@/app/automacao/templates/novo/NovoTemplateClient";
import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";

interface TemplatesListClientProps {
  initialData?: ListTemplatesResult;
}

export function TemplatesListClient({ initialData }: TemplatesListClientProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const showNewTemplate = searchParams.get("novo") === "true";
  
  const [loading, setLoading] = useState(!initialData);
  const [templates, setTemplates] = useState<Template[]>(initialData?.templates || []);
  const [total, setTotal] = useState(initialData?.total || 0);
  const [page, setPage] = useState(initialData?.page || 1);
  const [pageSize] = useState(initialData?.pageSize || 20);
  const [totalPages, setTotalPages] = useState(initialData?.totalPages || 0);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialog, setDeleteDialog] = useState<{ isOpen: boolean; templateId: string | null; templateName: string }>({
    isOpen: false,
    templateId: null,
    templateName: "",
  });
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    if (!initialData) {
      loadTemplates();
    }
  }, [page]);

  const loadTemplates = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await listTemplatesClient({
        page,
        pageSize,
      });
      setTemplates(result.templates);
      setTotal(result.total);
      setPage(result.page);
      setTotalPages(result.totalPages);
    } catch (err) {
      console.error("Erro ao carregar templates:", err);
      setError(err instanceof Error ? err.message : "Erro ao carregar templates");
    } finally {
      setLoading(false);
    }
  };

  const getTypeLabel = (type: TemplateType): string => {
    switch (type) {
      case TemplateType.Email:
        return "E-mail";
      case TemplateType.Oficio:
        return "Ofício";
      case TemplateType.Invite:
        return "Convite";
      case TemplateType.Generic:
        return "Genérico";
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
      await deleteTemplateClient(deleteDialog.templateId);
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

  const handleNewTemplateCancel = () => {
    router.push("/documentos");
  };

  const handleNewTemplateSuccess = async () => {
    router.push("/documentos");
    router.refresh();
    await loadTemplates();
  };

  return (
    <div className="space-y-6">
      {/* Formulário de Novo Template - aparece quando ?novo=true */}
      {showNewTemplate && (
        <div className="glass-card p-6 animate-slide-up">
          <div className="flex items-center justify-between mb-6">
            <h2 className="text-2xl font-semibold">Novo Template</h2>
            <Button
              variant="ghost"
              size="icon"
              onClick={handleNewTemplateCancel}
              className="rounded-lg"
            >
              <ArrowLeft className="w-4 h-4" />
            </Button>
          </div>
          <NovoTemplateClient onSuccess={handleNewTemplateSuccess} onCancel={handleNewTemplateCancel} />
        </div>
      )}

      {/* Lista de Templates - sempre visível */}
      <div>
        {error && (
          <div className="mb-4 rounded-md bg-red-50 dark:bg-red-900/20 p-4">
            <p className="text-sm text-red-800 dark:text-red-200">{error}</p>
          </div>
        )}

        <div className="mb-6">
          <p className="text-sm text-muted-foreground">
            {total} {total === 1 ? "template encontrado" : "templates encontrados"}
          </p>
        </div>

        {loading && templates.length === 0 ? (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground">Carregando templates...</p>
          </div>
        ) : templates.length === 0 ? (
          <div className="glass-card p-12 text-center">
            <p className="text-muted-foreground">Nenhum template encontrado.</p>
            <button
              onClick={() => router.push("/documentos?tab=templates&novo=true")}
              className="mt-4 inline-block text-primary hover:underline"
            >
              Criar primeiro template
            </button>
          </div>
        ) : (
          <>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              {templates.map((template) => (
                <div
                  key={template.templateId}
                  className="glass-card p-6 hover:shadow-md transition-shadow"
                >
                  <Link
                    href={`/automacao/templates/${template.templateId}`}
                    className="block"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <h3 className="text-lg font-semibold">
                        {template.name}
                      </h3>
                      {template.active ? (
                        <span className="inline-flex items-center rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 px-2.5 py-0.5 text-xs font-medium">
                          Ativo
                        </span>
                      ) : (
                        <span className="inline-flex items-center rounded-full bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200 px-2.5 py-0.5 text-xs font-medium">
                          Inativo
                        </span>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mb-2">
                      Tipo: {getTypeLabel(template.type)}
                    </p>
                    <p className="text-sm text-muted-foreground mb-4 line-clamp-3">
                      {template.body}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Criado em: {formatDate(template.createdAt)}
                    </p>
                  </Link>
                  <div className="mt-4 flex gap-2">
                    <Link
                      href={`/automacao/templates/${template.templateId}/editar`}
                      className="flex-1 text-center rounded-md border border-border bg-secondary/50 px-3 py-2 text-sm font-medium hover:bg-secondary/80 transition-colors"
                    >
                      Editar
                    </Link>
                    <button
                      onClick={(e) => {
                        e.preventDefault();
                        handleDeleteClick(template.templateId, template.name);
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
              title="Excluir Template"
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
                <Button
                  variant="ghost"
                  onClick={() => setPage(Math.max(1, page - 1))}
                  disabled={page === 1}
                >
                  Anterior
                </Button>
                <span className="text-sm text-muted-foreground">
                  Página {page} de {totalPages}
                </span>
                <Button
                  variant="ghost"
                  onClick={() => setPage(Math.min(totalPages, page + 1))}
                  disabled={page === totalPages}
                >
                  Próxima
                </Button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}

