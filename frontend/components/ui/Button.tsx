import { forwardRef, type ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type Variant = "primary" | "secondary" | "ghost";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
}

const variantClasses: Record<Variant, string> = {
  primary:
    "bg-accent text-accent-on font-bold hover:bg-accent-hover hover:shadow-[var(--shadow-md)]",
  secondary:
    "border border-border-default text-primary hover:bg-elevated",
  ghost: "text-primary hover:bg-elevated",
};

/** Pill button per the design system (PLAN §3.5). */
export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "primary", ...props }, ref) => (
    <button
      ref={ref}
      className={cn(
        "inline-flex items-center justify-center rounded-pill px-5 py-2.5 text-sm transition-all duration-150 disabled:cursor-not-allowed disabled:opacity-50",
        variantClasses[variant],
        className,
      )}
      {...props}
    />
  ),
);
Button.displayName = "Button";
