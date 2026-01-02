"use client";

import { useState, FormEvent } from "react";
import { Address } from "@/lib/types/contact";

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

  // Classes base para inputs
  const inputBaseClass = "w-full rounded-xl border bg-secondary/50 px-4 py-3 text-sm text-foreground placeholder:text-muted-foreground transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50";
  const inputErrorClass = "border-destructive/50 focus:ring-destructive/30 focus:border-destructive/50";
  const inputNormalClass = "border-border";
  const labelClass = "block text-sm font-medium text-foreground mb-2";

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {errors.general && (
        <div className="rounded-xl bg-destructive/10 border border-destructive/30 p-4">
          <p className="text-sm text-destructive">{errors.general}</p>
        </div>
      )}

      {/* Nome */}
      <div>
        <label htmlFor="firstName" className={labelClass}>
          Nome <span className="text-destructive">*</span>
        </label>
        <input
          type="text"
          id="firstName"
          value={formData.firstName}
          onChange={(e) => setFormData((prev) => ({ ...prev, firstName: e.target.value }))}
          className={`${inputBaseClass} ${errors.firstName ? inputErrorClass : inputNormalClass}`}
          required
        />
        {errors.firstName && <p className="mt-2 text-sm text-destructive">{errors.firstName}</p>}
      </div>

      {/* Sobrenome */}
      <div>
        <label htmlFor="lastName" className={labelClass}>
          Sobrenome
        </label>
        <input
          type="text"
          id="lastName"
          value={formData.lastName}
          onChange={(e) => setFormData((prev) => ({ ...prev, lastName: e.target.value }))}
          className={`${inputBaseClass} ${inputNormalClass}`}
        />
      </div>

      {/* Emails */}
      <div>
        <label className={labelClass}>Emails</label>
        {formData.emails.map((email, index) => (
          <div key={index} className="flex gap-2 mb-3 items-center">
            <input
              type="email"
              value={email}
              onChange={(e) => updateEmail(index, e.target.value)}
              placeholder="email@exemplo.com"
              className={`flex-1 ${inputBaseClass} ${
                errors[`emails.${index}` as keyof typeof errors] ? inputErrorClass : inputNormalClass
              }`}
            />
            {index === formData.emails.length - 1 && (
              <button
                type="button"
                onClick={addEmail}
                className="text-sm text-primary hover:text-primary/80 whitespace-nowrap font-medium transition-colors"
              >
                + Adicionar
              </button>
            )}
            {formData.emails.length > 1 && index < formData.emails.length - 1 && (
              <button
                type="button"
                onClick={() => removeEmail(index)}
                className="px-3 py-2 text-sm text-destructive hover:text-destructive/80 transition-colors"
              >
                Remover
              </button>
            )}
          </div>
        ))}
      </div>

      {/* Telefones */}
      <div>
        <label className={labelClass}>Telefones</label>
        {formData.phones.map((phone, index) => (
          <div key={index} className="flex gap-2 mb-3 items-center">
            <input
              type="tel"
              value={phone}
              onChange={(e) => updatePhone(index, e.target.value)}
              placeholder="(11) 99999-9999"
              className={`flex-1 ${inputBaseClass} ${
                errors[`phones.${index}` as keyof typeof errors] ? inputErrorClass : inputNormalClass
              }`}
            />
            {index === formData.phones.length - 1 && (
              <button
                type="button"
                onClick={addPhone}
                className="text-sm text-primary hover:text-primary/80 whitespace-nowrap font-medium transition-colors"
              >
                + Adicionar
              </button>
            )}
            {formData.phones.length > 1 && index < formData.phones.length - 1 && (
              <button
                type="button"
                onClick={() => removePhone(index)}
                className="px-3 py-2 text-sm text-destructive hover:text-destructive/80 transition-colors"
              >
                Remover
              </button>
            )}
          </div>
        ))}
      </div>

      {/* Empresa */}
      <div>
        <label htmlFor="company" className={labelClass}>
          Empresa
        </label>
        <input
          type="text"
          id="company"
          value={formData.company}
          onChange={(e) => setFormData((prev) => ({ ...prev, company: e.target.value }))}
          className={`${inputBaseClass} ${inputNormalClass}`}
        />
      </div>

      {/* Cargo */}
      <div>
        <label htmlFor="jobTitle" className={labelClass}>
          Cargo
        </label>
        <input
          type="text"
          id="jobTitle"
          value={formData.jobTitle}
          onChange={(e) => setFormData((prev) => ({ ...prev, jobTitle: e.target.value }))}
          className={`${inputBaseClass} ${inputNormalClass}`}
        />
      </div>

      {/* Endereço */}
      <div className="space-y-4 border-t border-border pt-6">
        <h3 className="text-sm font-semibold text-foreground">Endereço</h3>

        <div>
          <label htmlFor="street" className={labelClass}>
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
            className={`${inputBaseClass} ${inputNormalClass}`}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="city" className={labelClass}>
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
              className={`${inputBaseClass} ${inputNormalClass}`}
            />
          </div>

          <div>
            <label htmlFor="state" className={labelClass}>
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
              className={`${inputBaseClass} ${inputNormalClass}`}
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label htmlFor="zipCode" className={labelClass}>
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
              className={`${inputBaseClass} ${inputNormalClass}`}
            />
          </div>

          <div>
            <label htmlFor="country" className={labelClass}>
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
              className={`${inputBaseClass} ${inputNormalClass}`}
            />
          </div>
        </div>
      </div>

      {/* Botões */}
      <div className="flex gap-3 pt-6 border-t border-border">
        <button
          type="submit"
          disabled={isSubmitting}
          className="flex-1 rounded-xl bg-gradient-to-r from-primary to-accent px-6 py-3 text-sm font-medium text-primary-foreground shadow-lg shadow-primary/20 hover:shadow-primary/40 hover:scale-[1.02] disabled:opacity-60 disabled:cursor-not-allowed disabled:hover:scale-100 transition-all duration-300"
        >
          {isSubmitting ? "Salvando..." : submitLabel}
        </button>
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            disabled={isSubmitting}
            className="px-6 py-3 rounded-xl border border-border bg-secondary/50 text-sm font-medium text-foreground hover:bg-secondary disabled:opacity-60 disabled:cursor-not-allowed transition-all duration-300"
          >
            {cancelLabel}
          </button>
        )}
      </div>
    </form>
  );
}
