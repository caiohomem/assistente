"use client";

import { useState, useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import type { Contact } from "@/lib/types/contact";

interface SearchableContactSelectProps {
  contacts: Contact[];
  value: string;
  onChange: (contactId: string) => void;
  loading?: boolean;
  error?: string;
  placeholder?: string;
  className?: string;
}

export function SearchableContactSelect({
  contacts,
  value,
  onChange,
  loading = false,
  error,
  placeholder = "Selecione um contato",
  className = "",
}: SearchableContactSelectProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [highlightedIndex, setHighlightedIndex] = useState(-1);
  const [dropdownPosition, setDropdownPosition] = useState({ top: 0, left: 0, width: 0 });
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);

  const selectedContact = contacts.find((c) => c.contactId === value);

  // Filtrar contatos baseado no termo de busca
  const filteredContacts = contacts.filter((contact) => {
    if (!searchTerm.trim()) return true;
    const term = searchTerm.toLowerCase();
    return (
      contact.fullName.toLowerCase().includes(term) ||
      contact.company?.toLowerCase().includes(term) ||
      contact.emails.some((email) => email.toLowerCase().includes(term)) ||
      contact.phones.some((phone) => phone.includes(term))
    );
  });

  // Calcular posição do dropdown quando abrir ou quando a janela mudar
  useEffect(() => {
    const updatePosition = () => {
      if (isOpen && containerRef.current) {
        const rect = containerRef.current.getBoundingClientRect();
        setDropdownPosition({
          top: rect.bottom + window.scrollY + 4,
          left: rect.left + window.scrollX,
          width: rect.width,
        });
      }
    };

    if (isOpen) {
      updatePosition();
      window.addEventListener("scroll", updatePosition, true);
      window.addEventListener("resize", updatePosition);
      return () => {
        window.removeEventListener("scroll", updatePosition, true);
        window.removeEventListener("resize", updatePosition);
      };
    }
  }, [isOpen]);

  // Fechar quando clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        if (listRef.current && !listRef.current.contains(event.target as Node)) {
          setIsOpen(false);
          setSearchTerm("");
          setHighlightedIndex(-1);
        }
      }
    };

    if (isOpen) {
      document.addEventListener("mousedown", handleClickOutside);
      return () => document.removeEventListener("mousedown", handleClickOutside);
    }
  }, [isOpen]);

  // Scroll para o item destacado
  useEffect(() => {
    if (highlightedIndex >= 0 && listRef.current) {
      const items = listRef.current.children;
      if (items[highlightedIndex]) {
        items[highlightedIndex].scrollIntoView({
          block: "nearest",
          behavior: "smooth",
        });
      }
    }
  }, [highlightedIndex]);

  const handleSelect = (contactId: string) => {
    onChange(contactId);
    setIsOpen(false);
    setSearchTerm("");
    setHighlightedIndex(-1);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!isOpen && (e.key === "Enter" || e.key === " " || e.key === "ArrowDown")) {
      e.preventDefault();
      setIsOpen(true);
      return;
    }

    if (!isOpen) return;

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        setHighlightedIndex((prev) =>
          prev < filteredContacts.length - 1 ? prev + 1 : prev
        );
        break;
      case "ArrowUp":
        e.preventDefault();
        setHighlightedIndex((prev) => (prev > 0 ? prev - 1 : -1));
        break;
      case "Enter":
        e.preventDefault();
        if (highlightedIndex >= 0 && filteredContacts[highlightedIndex]) {
          handleSelect(filteredContacts[highlightedIndex].contactId);
        }
        break;
      case "Escape":
        e.preventDefault();
        setIsOpen(false);
        setSearchTerm("");
        setHighlightedIndex(-1);
        break;
    }
  };

  const displayValue = selectedContact
    ? `${selectedContact.fullName}${selectedContact.company ? ` (${selectedContact.company})` : ""}`
    : "";

  return (
    <div ref={containerRef} className={`relative ${className}`}>
      <div
        className={`w-full rounded-md border ${
          error
            ? "border-red-300 dark:border-red-700"
            : "border-zinc-300 dark:border-zinc-700"
        } bg-white dark:bg-zinc-800 text-zinc-900 dark:text-zinc-100 px-4 py-2 text-sm focus-within:border-indigo-500 dark:focus-within:border-indigo-500 focus-within:outline-none focus-within:ring-2 focus-within:ring-indigo-500`}
      >
        {loading ? (
          <div className="text-sm text-zinc-500 dark:text-zinc-400">Carregando contatos...</div>
        ) : (
          <div className="flex items-center justify-between cursor-pointer">
            <input
              ref={inputRef}
              type="text"
              value={isOpen ? searchTerm : displayValue}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setIsOpen(true);
                setHighlightedIndex(-1);
              }}
              onKeyDown={handleKeyDown}
              onFocus={() => {
                if (!isOpen) {
                  setIsOpen(true);
                  setSearchTerm("");
                }
              }}
              onClick={(e) => {
                e.stopPropagation();
                if (!isOpen) {
                  setIsOpen(true);
                  setSearchTerm("");
                }
              }}
              placeholder={placeholder}
              className="flex-1 bg-transparent border-none outline-none text-zinc-900 dark:text-zinc-100 placeholder-zinc-400 dark:placeholder-zinc-500 cursor-pointer"
            />
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                setIsOpen(!isOpen);
                if (!isOpen) {
                  setTimeout(() => inputRef.current?.focus(), 0);
                }
              }}
              className="flex items-center justify-center p-1 hover:bg-zinc-100 dark:hover:bg-zinc-700 rounded transition-colors"
              aria-label="Toggle dropdown"
            >
              <svg
                className={`w-5 h-5 text-zinc-400 transition-transform ${isOpen ? "rotate-180" : ""}`}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M19 9l-7 7-7-7"
                />
              </svg>
            </button>
          </div>
        )}
      </div>

      {isOpen && !loading && typeof window !== "undefined" && createPortal(
        <ul
          ref={listRef}
          className="fixed z-[99999] max-h-60 overflow-auto rounded-md border border-zinc-300 dark:border-zinc-700 bg-white dark:bg-zinc-800 shadow-lg"
          style={{
            top: `${dropdownPosition.top}px`,
            left: `${dropdownPosition.left}px`,
            width: `${dropdownPosition.width}px`,
          }}
        >
          {filteredContacts.length === 0 ? (
            <li className="px-4 py-2 text-sm text-zinc-500 dark:text-zinc-400">
              Nenhum contato encontrado
            </li>
          ) : (
            filteredContacts.map((contact, index) => (
              <li
                key={contact.contactId}
                onClick={() => handleSelect(contact.contactId)}
                onMouseEnter={() => setHighlightedIndex(index)}
                className={`px-4 py-2 text-sm cursor-pointer ${
                  contact.contactId === value
                    ? "bg-indigo-100 dark:bg-indigo-900 text-indigo-900 dark:text-indigo-100"
                    : highlightedIndex === index
                    ? "bg-zinc-100 dark:bg-zinc-700"
                    : "text-zinc-900 dark:text-zinc-100"
                } hover:bg-zinc-100 dark:hover:bg-zinc-700`}
              >
                <div className="font-medium">{contact.fullName}</div>
                {contact.company && (
                  <div className="text-xs text-zinc-500 dark:text-zinc-400">
                    {contact.company}
                  </div>
                )}
                {contact.emails.length > 0 && (
                  <div className="text-xs text-zinc-500 dark:text-zinc-400">
                    {contact.emails[0]}
                  </div>
                )}
              </li>
            ))
          )}
        </ul>,
        document.body
      )}

      {error && (
        <p className="mt-1 text-sm text-red-600 dark:text-red-400">{error}</p>
      )}
    </div>
  );
}

