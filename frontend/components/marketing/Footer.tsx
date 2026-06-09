import Link from "next/link";
import { useTranslations } from "next-intl";
import { LanguageToggle } from "@/components/shared/LanguageToggle";

export function Footer() {
  const t = useTranslations("footer");
  const nav = useTranslations("nav");

  return (
    <footer className="border-t border-border-subtle bg-base">
      <div className="mx-auto grid max-w-6xl gap-10 px-6 py-16 md:grid-cols-3">
        <div>
          <div className="flex items-center gap-2 font-bold text-primary">
            <span aria-hidden className="inline-block h-6 w-6 rounded-pill bg-accent" />
            Ghế Đầy
          </div>
          <p className="mt-4 max-w-xs text-sm text-secondary">{t("tagline")}</p>
        </div>

        <div className="flex flex-col gap-2 text-sm text-secondary">
          <span className="text-xs font-semibold uppercase tracking-[0.08em] text-tertiary">
            {t("product")}
          </span>
          <Link href="#features" className="hover:text-primary">{nav("features")}</Link>
          <Link href="/pricing" className="hover:text-primary">{nav("pricing")}</Link>
          <Link href="#faq" className="hover:text-primary">{nav("faq")}</Link>
        </div>

        <div className="flex flex-col items-start gap-4">
          <span className="text-xs font-semibold uppercase tracking-[0.08em] text-tertiary">
            {t("language")}
          </span>
          <LanguageToggle />
        </div>
      </div>
      <div className="border-t border-border-subtle py-6 text-center text-xs text-tertiary">
        © {new Date().getFullYear()} Ghế Đầy. {t("rights")}
      </div>
    </footer>
  );
}
