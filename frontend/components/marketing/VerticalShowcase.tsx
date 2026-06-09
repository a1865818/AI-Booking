"use client";

import { useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { cn } from "@/lib/utils";

/**
 * Adding a 4th vertical is a single entry in this array — no new code (mirrors the backend's
 * vertical-config-not-code principle, PLAN §2.8 / §3.7).
 */
const verticals = [
  {
    key: "nail",
    label: "Nail Salon",
    headline: "Books services in Vietnamese and English",
    body: "Customers text the way they speak. Ghế Đầy understands the service, offers open chairs, and takes a deposit to lock it in.",
  },
  {
    key: "restaurant",
    label: "Restaurant",
    headline: "Asks party size, then seats the right table",
    body: "“Table for 4 at 7?” is all it takes. Larger parties are sent a deposit link automatically; everyone else gets an instant confirmation.",
  },
  {
    key: "barbershop",
    label: "Barbershop",
    headline: "Keeps every chair full all day",
    body: "Walk-ins join a waitlist over SMS and get offered the next open slot the moment a chair frees up.",
  },
] as const;

export function VerticalShowcase() {
  const [active, setActive] = useState<(typeof verticals)[number]["key"]>("nail");
  const current = verticals.find((v) => v.key === active)!;

  return (
    <section id="features" className="mx-auto max-w-6xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">
        One assistant. Every seat-based business.
      </h2>

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
            <h3 className="text-2xl font-semibold text-primary">
              {current.headline}
            </h3>
            <p className="mt-4 text-base leading-7 text-secondary">{current.body}</p>
          </motion.div>
        </AnimatePresence>

        <div className="aspect-[4/3] w-full rounded-xl bg-surface shadow-[var(--shadow-lg)]">
          <div className="flex h-full items-center justify-center text-sm text-tertiary">
            {current.label} demo
          </div>
        </div>
      </div>
    </section>
  );
}
