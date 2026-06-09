"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

export function Faq() {
  const t = useTranslations("faq");
  const faqs = t.raw("items") as { q: string; a: string }[];
  const [open, setOpen] = useState<number | null>(0);

  return (
    <section id="faq" className="mx-auto max-w-3xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">{t("heading")}</h2>
      <div className="mt-10 flex flex-col divide-y divide-border-subtle">
        {faqs.map((faq, i) => (
          <div key={faq.q}>
            <button
              type="button"
              aria-expanded={open === i}
              onClick={() => setOpen(open === i ? null : i)}
              className="flex w-full items-center justify-between py-4 text-left"
            >
              <span className="font-semibold text-primary">{faq.q}</span>
              <ChevronDown
                className={cn(
                  "h-4 w-4 text-secondary transition-transform duration-150",
                  open === i && "rotate-180",
                )}
              />
            </button>
            <div
              className={cn(
                "grid overflow-hidden transition-all duration-250",
                open === i ? "grid-rows-[1fr] pb-4" : "grid-rows-[0fr]",
              )}
            >
              <p className="min-h-0 text-sm leading-6 text-secondary">{faq.a}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
