"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";

/**
 * Per-conversation AI on/off. Flipping it off is the "human takeover" handoff (PLAN §3.8);
 * the change is pushed to all dashboards via the AiToggled SignalR event.
 */
export function AiToggle({
  initial = true,
  onChange,
}: {
  initial?: boolean;
  onChange?: (enabled: boolean) => void;
}) {
  const [enabled, setEnabled] = useState(initial);

  return (
    <button
      type="button"
      role="switch"
      aria-checked={enabled}
      onClick={() => {
        const next = !enabled;
        setEnabled(next);
        onChange?.(next);
      }}
      className="flex items-center gap-2 text-sm text-secondary"
    >
      <span
        className={cn(
          "relative inline-flex h-6 w-11 items-center rounded-pill transition-colors duration-150",
          enabled ? "bg-accent" : "bg-elevated",
        )}
      >
        <span
          className={cn(
            "inline-block h-4 w-4 transform rounded-pill bg-accent-on transition-transform duration-150",
            enabled ? "translate-x-6" : "translate-x-1",
          )}
        />
      </span>
      AI {enabled ? "on" : "paused"}
    </button>
  );
}
