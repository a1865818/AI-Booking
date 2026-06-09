"use client";

import { useTransition } from "react";
import { useLocale } from "next-intl";
import { useRouter } from "next/navigation";
import { persistLocale } from "@/lib/locale";
import { cn } from "@/lib/utils";

const locales = ["en", "vi"] as const;

/** EN/VI toggle: sets the NEXT_LOCALE cookie and refreshes so server components re-render copy. */
export function LanguageToggle() {
  const active = useLocale();
  const router = useRouter();
  const [pending, startTransition] = useTransition();

  function select(locale: string) {
    if (locale === active) return;
    persistLocale(locale);
    startTransition(() => router.refresh());
  }

  return (
    <div
      role="group"
      aria-label="Language"
      aria-busy={pending}
      className="inline-flex items-center gap-1 rounded-pill border border-border-default p-0.5"
    >
      {locales.map((locale) => (
        <button
          key={locale}
          type="button"
          aria-pressed={active === locale}
          onClick={() => select(locale)}
          className={cn(
            "rounded-pill px-3 py-1 text-xs font-semibold uppercase tracking-[0.08em] transition-colors",
            active === locale ? "bg-accent text-accent-on" : "text-secondary hover:bg-elevated",
          )}
        >
          {locale}
        </button>
      ))}
    </div>
  );
}
