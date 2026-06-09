"use client";

import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";

type Key = "nail" | "restaurant" | "barber";

export function VerticalShowcase() {
  const t = useTranslations("showcase");
  const [active, setActive] = useState<Key>("nail");

  // Adding a 4th vertical is a single entry here + 3 message keys — no new code.
  const verticals: { key: Key; label: string; headline: string; body: string }[] = [
    { key: "nail", label: t("nailLabel"), headline: t("nailHeadline"), body: t("nailBody") },
    { key: "restaurant", label: t("restaurantLabel"), headline: t("restaurantHeadline"), body: t("restaurantBody") },
    { key: "barber", label: t("barberLabel"), headline: t("barberHeadline"), body: t("barberBody") },
  ];
  const current = verticals.find((v) => v.key === active)!;

  return (
    <section id="features" className="mx-auto max-w-6xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">{t("heading")}</h2>

      <div className="mt-10 flex justify-center gap-2">
        {verticals.map((v) => (
          <button
            key={v.key}
            type="button"
            aria-pressed={active === v.key}
            onClick={() => setActive(v.key)}
            className={cn(
              "rounded-pill px-5 py-2 text-sm font-semibold transition-colors",
              active === v.key
                ? "bg-accent text-accent-on"
                : "border border-border-default text-secondary hover:bg-elevated",
            )}
          >
            {v.label}
          </button>
        ))}
      </div>

      <div className="mt-12 grid items-center gap-10 md:grid-cols-2">
        <AnimatePresence mode="wait">
          <motion.div
            key={current.key}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
            transition={{ duration: 0.3, ease: "easeOut" }}
          >
            <h3 className="text-2xl font-semibold text-primary">{current.headline}</h3>
            <p className="mt-4 text-base leading-7 text-secondary">{current.body}</p>
          </motion.div>
        </AnimatePresence>

        <div className="aspect-[4/3] w-full rounded-xl bg-surface shadow-[var(--shadow-lg)]">
          <div className="flex h-full items-center justify-center text-sm text-tertiary">
            {current.label} {t("demoSuffix")}
          </div>
        </div>
      </div>
    </section>
  );
}
