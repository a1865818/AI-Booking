import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type Tone = "accent" | "warning" | "neutral" | "error";

const toneClasses: Record<Tone, string> = {
  accent: "bg-accent-subtle text-accent",
  warning: "bg-[rgba(251,191,36,0.12)] text-warning",
  neutral: "bg-elevated text-secondary",
  error: "bg-[rgba(248,113,113,0.12)] text-error",
};

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  tone?: Tone;
}

/** Status pill using Label type — uppercase, tracked (PLAN §3.5). */
export function Badge({ className, tone = "neutral", ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-pill px-2.5 py-0.5 text-xs font-semibold uppercase tracking-[0.08em]",
        toneClasses[tone],
        className,
      )}
      {...props}
    />
  );
}
