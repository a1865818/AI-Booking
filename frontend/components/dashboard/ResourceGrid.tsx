"use client";

import { motion } from "framer-motion";
import { cn } from "@/lib/utils";

export type ResourceCell = {
  id: string;
  name: string;
  state: "free" | "pending" | "confirmed";
};

const stateClasses: Record<ResourceCell["state"], string> = {
  free: "text-secondary",
  pending: "text-warning",
  confirmed: "text-accent",
};

const stateLabel: Record<ResourceCell["state"], string> = {
  free: "Free",
  pending: "Pending deposit",
  confirmed: "Confirmed",
};

/**
 * The dashboard home grid. The heading label is driven by the tenant's
 * `vertical_config.resource_label_plural` (Chairs / Tables / Bays) — never hardcoded
 * (PLAN §3.8). New bookings pulse emerald via the `isNew` flag.
 */
export function ResourceGrid({
  resourceLabelPlural,
  resources,
  newestResourceId,
}: {
  resourceLabelPlural: string;
  resources: ResourceCell[];
  newestResourceId?: string | null;
}) {
  return (
    <section>
      <h2 className="text-lg font-semibold text-primary">{resourceLabelPlural}</h2>
      <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {resources.map((r) => (
          <motion.div
            key={r.id}
            layout
            initial={r.id === newestResourceId ? { y: -16, opacity: 0 } : false}
            animate={{ y: 0, opacity: 1 }}
            transition={{ type: "spring", stiffness: 300, damping: 30 }}
            className={cn(
              "rounded-lg bg-surface p-4 transition-shadow hover:shadow-[var(--shadow-md)]",
              r.id === newestResourceId &&
                "ring-2 ring-accent [animation:pulse_600ms_ease-out]",
            )}
          >
            <p className="font-semibold text-primary">{r.name}</p>
            <p className={cn("mt-1 text-xs font-semibold uppercase tracking-[0.08em]", stateClasses[r.state])}>
              {stateLabel[r.state]}
            </p>
          </motion.div>
        ))}
      </div>
    </section>
  );
}
