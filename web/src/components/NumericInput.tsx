"use client";

import { useMemo, useState } from "react";
import { useLocale } from "next-intl";
import { NumericFormat } from "react-number-format";

interface NumericInputProps {
  value?: number;
  onValueChange: (value: number) => void;
  decimalScale?: number;
  placeholder?: string;
  className?: string;
  allowNegative?: boolean;
  disabled?: boolean;
}

export default function NumericInput({
  value,
  onValueChange,
  decimalScale = 2,
  placeholder,
  className,
  allowNegative = false,
  disabled = false,
}: NumericInputProps) {
  const locale = useLocale();
  const [focused, setFocused] = useState(false);

  const numberFormatOptions = useMemo(() => {
    const parts = new Intl.NumberFormat(locale).formatToParts(1000.1);
    return {
      decimal: parts.find((part) => part.type === "decimal")?.value ?? ".",
      group: parts.find((part) => part.type === "group")?.value ?? ",",
    };
  }, [locale]);

  const sanitizedValue = value ?? undefined;
  const displayValue = sanitizedValue === 0 && !focused ? undefined : sanitizedValue;
  const shouldFixedDecimal = !focused && displayValue !== undefined;

  return (
    <NumericFormat
      value={displayValue}
      decimalScale={decimalScale}
      fixedDecimalScale={shouldFixedDecimal}
      allowNegative={allowNegative}
      thousandSeparator={numberFormatOptions.group}
      decimalSeparator={numberFormatOptions.decimal}
      placeholder={placeholder}
      disabled={disabled}
      className={className}
      onValueChange={(values) => onValueChange(values.floatValue ?? 0)}
      onFocus={() => setFocused(true)}
      onBlur={() => setFocused(false)}
    />
  );
}
