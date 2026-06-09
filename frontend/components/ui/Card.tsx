import type { HTMLAttributes } from "react";
import { cn } from "@/lib/utils";

/** Surface card — no raw border, lifts on hover (PLAN §3.5). */
export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn(
        "rounded-lg bg-surface p-5 transition-shadow duration-150 hover:shadow-[var(--shadow-md)]",
        className,
      )}
      {...props}
    />
  );
}
