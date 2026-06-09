"use client";

import { motion } from "framer-motion";
import { cn } from "@/lib/utils";

interface FeatureSectionProps {
  eyebrow: string;
  title: string;
  body: string;
  reversed?: boolean;
}

export function FeatureSection({
  eyebrow,
  title,
  body,
  reversed,
}: FeatureSectionProps) {
  return (
    <section className="mx-auto max-w-6xl px-6 py-16">
      <div
        className={cn(
          "grid items-center gap-10 md:grid-cols-2",
          reversed && "md:[&>*:first-child]:order-2",
        )}
      >
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, margin: "-100px" }}
          transition={{ duration: 0.5, ease: "easeOut" }}
        >
          <p className="text-xs font-semibold uppercase tracking-[0.08em] text-accent">
            {eyebrow}
          </p>
          <h3 className="mt-3 text-2xl font-semibold text-primary">{title}</h3>
          <p className="mt-4 text-base leading-7 text-secondary">{body}</p>
        </motion.div>

        <div className="aspect-[3/2] w-full rounded-xl bg-surface shadow-[var(--shadow-md)]" />
      </div>
    </section>
  );
}
