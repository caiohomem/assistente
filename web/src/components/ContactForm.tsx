"use client";

import { useState, FormEvent } from "react";
import { Address, CreateContactRequest, UpdateContactRequest } from "@/lib/types/contact";

export interface ContactFormData {
  firstName: string;
  lastName: string;
  emails: string[];
  phones: string[];
  jobTitle: string;
  company: string;
  address: Address;
}

export interface ContactFormProps {
  initialData?: Partial<ContactFormData>;
  onSubmit: (data: ContactFormData) => Promise<void>;
  onCancel?: () => void;
  submitLabel?: string;
  cancelLabel?: string;
}

export function ContactForm({
  initialData,
  onSubmit,
  onCancel,
  submitLabel = "Salvar",
  cancelLabel = "Cancelar",
}: ContactFormProps) {
  const [formData, setFormData] = useState<ContactFormData>({
    firstName: initialData?.firstName || "",
    lastName: initialData?.lastName || "",
    emails: initialData?.emails || [""],
    phones: initialData?.phones || [""],
    jobTitle: initialData?.jobTitle || "",
    company: initialData?.company || "",
    address: {
      street: initialData?.address?.street || "",
      city: initialData?.address?.city || "",
      state: initialData?.address?.state || "",
      zipCode: initialData?.address?.zipCode || "",
      country: initialData?.address?.country || "",
    },
  });

  const [errors, setErrors] = useState<Partial<Record<keyof ContactFormData | "general", string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const validate = (): boolean => {
    const newErrors: typeof errors = {};

    if (!formData.firstName.trim()) {
      newErrors.firstName = "Nome é obrigatório";
    }

    // Validação de emails
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    formData.emails.forEach((email, index) => {
      if (email.trim() && !emailRegex.test(email.trim())) {
        newErrors[`emails.${index}` as keyof typeof errors] = "Email inválido";
      }
    });

    // Validação de telefones (formato básico)
    const phoneRegex = /^[\d\s\-\+\(\)]+$/;
    formData.phones.forEach((phone, index) => {
      if (phone.trim() && !phoneRegex.test(phone.trim())) {
        newErrors[`phones.${index}` as keyof typeof errors] = "Telefone inválido";
      }
    });

    // Validação: pelo menos um email ou telefone é obrigatório
    const hasValidEmail = formData.emails.some((email) => email.trim() && emailRegex.test(email.trim()));
    const hasValidPhone = formData.phones.some((phone) => phone.trim() && phoneRegex.test(phone.trim()));
    
    if (!hasValidEmail && !hasValidPhone) {
      newErrors.general = "O contato deve ter pelo menos um email ou telefone";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setErrors({});

    if (!validate()) {
      return;
    }

    setIsSubmitting(true);
    try {
      // Remove emails e telefones vazios antes de enviar
      const dataToSubmit: ContactFormData = {
        ...formData,
        emails: formData.emails.filter((e) => e.trim()),
        phones: formData.phones.filter((p) => p.trim()),
        address: {
          street: formData.address.street || undefined,
          city: formData.address.city || undefined,
          state: formData.address.state || undefined,
          zipCode: formData.address.zipCode || undefined,
          country: formData.address.country || undefined,
        },
      };

      await onSubmit(dataToSubmit);
    } catch (error) {
      setErrors({
        general: error instanceof Error ? error.message : "Erro ao salvar contato",
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const addEmail = () => {
    setFormData((prev) => ({
      ...prev,
      emails: [...prev.emails, ""],
    }));
  };

  const removeEmail = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      emails: prev.emails.filter((_, i) => i !== index),
    }));
  };

  const updateEmail = (index: number, value: string) => {
    setFormData((prev) => ({
      ...prev,
      emails: prev.emails.map((e, i) => (i === index ? value : e)),
    }));
  };

  const addPhone = () => {
    setFormData((prev) => ({
      ...prev,
      phones: [...prev.phones, ""],
    }));
  };

  const removePhone = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      phones: prev.phones.filter((_, i) => i !== index),
    }));
  };

  const updatePhone = (index: number, value: string) => {
    setFormData((prev) => ({
      ...prev,
      phones: prev.phones.map((p, i) => (i === index ? value : p)),
    }));
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {errors.general && (
        <div className="rounded-md bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-4">
          <p className="text-sm text-red-800 dark:text-red-200">{errors.general}</p>
        </div>
      )}

      {/* Nome */}
      <div>
        <label htmlFor="firstName" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
          Nome <span className="text-red-500 dark:text-red-400">*</span>
        </label>
        <input
          type="text"
          id="firstName"
          value={formData.firstName}
          onChange={(e) => setFormData((prev) => ({ ...prev, firstName: e.target.value }))}
          className={`w-full rounded-md border px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 ${
            errors.firstName 
              ? "border-red-300 dark:border-red-600" 
              : "border-zinc-300 dark:border-zinc-600"
          } focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400`}
          required
        />
        {errors.firstName && <p className="mt-1 text-sm text-red-600 dark:text-red-400">{errors.firstName}</p>}
      </div>

      {/* Sobrenome */}
      <div>
        <label htmlFor="lastName" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
          Sobrenome
        </label>
        <input
          type="text"
          id="lastName"
          value={formData.lastName}
          onChange={(e) => setFormData((prev) => ({ ...prev, lastName: e.target.value }))}
          className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
        />
      </div>

      {/* Emails */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Emails</label>
        {formData.emails.map((email, index) => (
          <div key={index} className="flex gap-2 mb-2 items-center">
            <input
              type="email"
              value={email}
              onChange={(e) => updateEmail(index, e.target.value)}
              placeholder="email@exemplo.com"
              className={`flex-1 rounded-md border px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 ${
                errors[`emails.${index}` as keyof typeof errors]
                  ? "border-red-300 dark:border-red-600"
                  : "border-zinc-300 dark:border-zinc-600"
              } focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400`}
            />
            {index === formData.emails.length - 1 && (
              <button
                type="button"
                onClick={addEmail}
                className="text-sm text-black dark:text-zinc-300 hover:underline whitespace-nowrap"
              >
                + Adicionar email
              </button>
            )}
            {formData.emails.length > 1 && index < formData.emails.length - 1 && (
              <button
                type="button"
                onClick={() => removeEmail(index)}
                className="px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
              >
                Remover
              </button>
            )}
          </div>
        ))}
      </div>

      {/* Telefones */}
      <div>
        <label className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">Telefones</label>
        {formData.phones.map((phone, index) => (
          <div key={index} className="flex gap-2 mb-2 items-center">
            <input
              type="tel"
              value={phone}
              onChange={(e) => updatePhone(index, e.target.value)}
              placeholder="(11) 99999-9999"
              className={`flex-1 rounded-md border px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 ${
                errors[`phones.${index}` as keyof typeof errors]
                  ? "border-red-300 dark:border-red-600"
                  : "border-zinc-300 dark:border-zinc-600"
              } focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400`}
            />
            {index === formData.phones.length - 1 && (
              <button
                type="button"
                onClick={addPhone}
                className="text-sm text-black dark:text-zinc-300 hover:underline whitespace-nowrap"
              >
                + Adicionar telefone
              </button>
            )}
            {formData.phones.length > 1 && index < formData.phones.length - 1 && (
              <button
                type="button"
                onClick={() => removePhone(index)}
                className="px-3 py-2 text-sm text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
              >
                Remover
              </button>
            )}
          </div>
        ))}
      </div>

      {/* Empresa */}
      <div>
        <label htmlFor="company" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
          Empresa
        </label>
        <input
          type="text"
          id="company"
          value={formData.company}
          onChange={(e) => setFormData((prev) => ({ ...prev, company: e.target.value }))}
          className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
        />
      </div>

      {/* Cargo */}
      <div>
        <label htmlFor="jobTitle" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
          Cargo
        </label>
        <input
          type="text"
          id="jobTitle"
          value={formData.jobTitle}
          onChange={(e) => setFormData((prev) => ({ ...prev, jobTitle: e.target.value }))}
          className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
        />
      </div>

      {/* Endereço */}
      <div className="space-y-4 border-t border-zinc-200 dark:border-zinc-700 pt-4">
        <h3 className="text-sm font-medium text-zinc-700 dark:text-zinc-300">Endereço</h3>

        <div>
          <label htmlFor="street" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
            Rua
          </label>
          <input
            type="text"
            id="street"
            value={formData.address.street || ""}
            onChange={(e) =>
              setFormData((prev) => ({
                ...prev,
                address: { ...prev.address, street: e.target.value },
              }))
            }
            className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="city" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
              Cidade
            </label>
            <input
              type="text"
              id="city"
              value={formData.address.city || ""}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  address: { ...prev.address, city: e.target.value },
                }))
              }
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
            />
          </div>

          <div>
            <label htmlFor="state" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
              Estado
            </label>
            <input
              type="text"
              id="state"
              value={formData.address.state || ""}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  address: { ...prev.address, state: e.target.value },
                }))
              }
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="zipCode" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
              CEP
            </label>
            <input
              type="text"
              id="zipCode"
              value={formData.address.zipCode || ""}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  address: { ...prev.address, zipCode: e.target.value },
                }))
              }
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
            />
          </div>

          <div>
            <label htmlFor="country" className="block text-sm font-medium text-zinc-700 dark:text-zinc-300 mb-1">
              País
            </label>
            <input
              type="text"
              id="country"
              value={formData.address.country || ""}
              onChange={(e) =>
                setFormData((prev) => ({
                  ...prev,
                  address: { ...prev.address, country: e.target.value },
                }))
              }
              className="w-full rounded-md border border-zinc-300 dark:border-zinc-600 px-3 py-2 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 focus:border-black dark:focus:border-zinc-400 focus:outline-none focus:ring-1 focus:ring-black dark:focus:ring-zinc-400"
            />
          </div>
        </div>
      </div>

      {/* Botões */}
      <div className="flex gap-3 pt-4 border-t border-zinc-200 dark:border-zinc-700">
        <button
          type="submit"
          disabled={isSubmitting}
          className="flex-1 rounded-md bg-black dark:bg-zinc-700 px-4 py-2 text-white dark:text-zinc-100 hover:bg-zinc-800 dark:hover:bg-zinc-600 disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
        >
          {isSubmitting ? "Salvando..." : submitLabel}
        </button>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            disabled={isSubmitting}
            className="px-4 py-2 rounded-md border border-zinc-300 dark:border-zinc-600 bg-white dark:bg-zinc-700 text-zinc-900 dark:text-zinc-100 hover:bg-zinc-50 dark:hover:bg-zinc-600 disabled:opacity-60 disabled:cursor-not-allowed transition-colors"
          >
            {cancelLabel}
          </button>
        )}
      </div>
    </form>
  );
}

