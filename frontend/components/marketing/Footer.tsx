import Link from "next/link";
import { LanguageToggle } from "@/components/shared/LanguageToggle";

export function Footer() {
  return (
    <footer className="border-t border-border-subtle bg-base">
      <div className="mx-auto grid max-w-6xl gap-10 px-6 py-16 md:grid-cols-3">
        <div>
          <div className="flex items-center gap-2 font-bold text-primary">
            <span aria-hidden className="inline-block h-6 w-6 rounded-pill bg-accent" />
            Ghế Đầy
          </div>
          <p className="mt-4 max-w-xs text-sm text-secondary">
            Your AI receptionist. Books every seat, in every language.
          </p>
        </div>

        <div className="flex flex-col gap-2 text-sm text-secondary">
          <span className="text-xs font-semibold uppercase tracking-[0.08em] text-tertiary">
            Product
          </span>
          <Link href="#features" className="hover:text-primary">Features</Link>
          <Link href="/pricing" className="hover:text-primary">Pricing</Link>
          <Link href="#faq" className="hover:text-primary">FAQ</Link>
        </div>

        <div className="flex flex-col items-start gap-4">
          <span className="text-xs font-semibold uppercase tracking-[0.08em] text-tertiary">
            Language
          </span>
          <LanguageToggle />
        </div>
      </div>
      <div className="border-t border-border-subtle py-6 text-center text-xs text-tertiary">
        © {new Date().getFullYear()} Ghế Đầy. All rights reserved.
      </div>
    </footer>
  );
}
