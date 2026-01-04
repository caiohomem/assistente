"use client";

import { type EmailTemplate } from "@/lib/api/emailTemplatesApiClient";
import { EmailTemplateType } from "@/lib/types/emailTemplates";

interface EmailTemplateDetailsClientProps {
  template: EmailTemplate;
}

export function EmailTemplateDetailsClient({ template }: EmailTemplateDetailsClientProps) {
  const getTypeLabel = (type: EmailTemplateType): string => {
    switch (type) {
      case EmailTemplateType.UserCreated:
        return "Usuário Criado";
      case EmailTemplateType.PasswordReset:
        return "Redefinição de Senha";
      case EmailTemplateType.Welcome:
        return "Bem-vindo";
      case EmailTemplateType.AgreementProposal:
        return "Acordo - Proposta";
      case EmailTemplateType.AgreementReminder:
        return "Acordo - Lembrete";
      case EmailTemplateType.AgreementApproved:
        return "Acordo - Aprovado";
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

  return (
    <div className="space-y-6">
      {/* Informações Gerais */}
      <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
        <div className="flex items-start justify-between mb-4">
          <div>
            <h2 className="text-2xl font-bold text-zinc-900 dark:text-zinc-100 mb-2">
              {template.name}
            </h2>
            <div className="flex items-center gap-3">
              {template.isActive ? (
                <span className="inline-flex items-center rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 px-3 py-1 text-sm font-medium">
                  Ativo
                </span>
              ) : (
                <span className="inline-flex items-center rounded-full bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200 px-3 py-1 text-sm font-medium">
                  Inativo
                </span>
              )}
              <span className="text-sm text-zinc-500 dark:text-zinc-400">
                Tipo: {getTypeLabel(template.templateType)}
              </span>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mt-4">
          <div>
            <p className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Criado em</p>
            <p className="text-sm text-zinc-600 dark:text-zinc-400">{formatDate(template.createdAt)}</p>
          </div>
          {template.updatedAt && (
            <div>
              <p className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Atualizado em</p>
              <p className="text-sm text-zinc-600 dark:text-zinc-400">{formatDate(template.updatedAt)}</p>
            </div>
          )}
        </div>
      </div>

      {/* Assunto */}
      <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
        <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-2">Assunto</h3>
        <p className="text-zinc-700 dark:text-zinc-300">{template.subject}</p>
      </div>

      {/* Placeholders */}
      {template.placeholders.length > 0 && (
        <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
          <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-2">
            Placeholders Detectados
          </h3>
          <div className="flex flex-wrap gap-2">
            {template.placeholders.map((placeholder) => (
              <span
                key={placeholder}
                className="inline-flex items-center rounded-md bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200 px-2.5 py-0.5 text-sm font-medium"
              >
                {"{{ " + placeholder + " }}"}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Preview do HTML */}
      <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
        <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-4">Preview do HTML</h3>
        <div className="border border-zinc-200 dark:border-zinc-700 rounded-md bg-zinc-50 dark:bg-zinc-900">
          <iframe
            title="Preview do template"
            srcDoc={template.htmlBody}
            className="w-full min-h-[400px] rounded-md border-0"
            sandbox="allow-same-origin"
          />
        </div>
      </div>

      {/* Código HTML */}
      <div className="bg-white dark:bg-zinc-800 rounded-lg border border-zinc-200 dark:border-zinc-700 shadow-sm p-6">
        <h3 className="text-lg font-semibold text-zinc-900 dark:text-zinc-100 mb-4">Código HTML</h3>
        <pre className="bg-zinc-900 dark:bg-zinc-950 text-zinc-100 p-4 rounded-md overflow-x-auto text-sm">
          <code>{template.htmlBody}</code>
        </pre>
      </div>
    </div>
  );
}

