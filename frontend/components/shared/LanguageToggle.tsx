"use client";

import { useState } from "react";
import { locales, type Locale } from "@/lib/i18n";
import { cn } from "@/lib/utils";

/**
 * EN/VI pill toggle. Local state for the scaffold; Phase 6 lifts this into the next-intl
 * locale router.
 */
export function LanguageToggle({
  onChange,
}: {
  onChange?: (locale: Locale) => void;
}) {
  const [active, setActive] = useState<Locale>("en");

  return (
    <div
      role="group"
      aria-label="Language"
      className="inline-flex items-center gap-1 rounded-pill border border-border-default p-0.5"
    >
      {locales.map((locale) => (
        <button
          key={locale}
          type="button"
          aria-pressed={active === locale}
          onClick={() => {
            setActive(locale);
            onChange?.(locale);
          }}
          className={cn(
            "rounded-pill px-3 py-1 text-xs font-semibold uppercase tracking-[0.08em] transition-colors",
            active === locale
              ? "bg-accent text-accent-on"
              : "text-secondary hover:bg-elevated",
          )}
        >
          {locale}
        </button>
      ))}
    </div>
  );
}
