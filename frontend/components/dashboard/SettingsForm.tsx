"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/Button";

const tabs = [
  "Offerings",
  "Resources",
  "Hours",
  "Deposit",
  "AI Persona",
  "Stripe Connect",
] as const;

/**
 * Settings tabs. The "Resources" tab label adapts to the tenant's vertical_config
 * (Chairs / Tables / Bays) — passed in as `resourceLabelPlural` (PLAN §3.8).
 */
export function SettingsForm({
  resourceLabelPlural = "Resources",
}: {
  resourceLabelPlural?: string;
}) {
  const [active, setActive] = useState<(typeof tabs)[number]>("Offerings");

  const label = (tab: (typeof tabs)[number]) =>
    tab === "Resources" ? resourceLabelPlural : tab;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap gap-2">
        {tabs.map((tab) => (
          <button
            key={tab}
            type="button"
            aria-pressed={active === tab}
            onClick={() => setActive(tab)}
            className={cn(
              "rounded-pill px-4 py-1.5 text-sm transition-colors",
              active === tab
                ? "bg-accent text-accent-on"
                : "border border-border-default text-secondary hover:bg-elevated",
            )}
          >
            {label(tab)}
          </button>
        ))}
      </div>

      <div className="rounded-lg bg-surface p-6">
        <p className="text-sm text-secondary">
          {label(active)} settings — form fields land in Phase 5.
        </p>
        <Button className="mt-4" disabled>
          Save
        </Button>
      </div>
    </div>
  );
}
