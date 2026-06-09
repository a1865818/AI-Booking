"use client";

import Link from "next/link";
import { LanguageToggle } from "@/components/shared/LanguageToggle";
import { Button } from "@/components/ui/Button";

const links = [
  { href: "#features", label: "Features" },
  { href: "#how", label: "How it works" },
  { href: "/pricing", label: "Pricing" },
  { href: "#faq", label: "FAQ" },
];

export function NavBar() {
  return (
    <header className="fixed inset-x-0 top-0 z-50 border-b border-border-subtle bg-base/80 backdrop-blur-md">
      <nav className="mx-auto flex h-16 max-w-6xl items-center justify-between px-6">
        <Link href="/" className="flex items-center gap-2 font-bold text-primary">
          <span
            aria-hidden
            className="inline-block h-6 w-6 rounded-pill bg-accent"
          />
          Ghế Đầy
        </Link>

        <ul className="hidden items-center gap-8 md:flex">
          {links.map((link) => (
            <li key={link.href}>
              <Link
                href={link.href}
                className="text-sm text-secondary transition-colors hover:text-primary"
              >
                {link.label}
              </Link>
            </li>
          ))}
        </ul>

        <div className="flex items-center gap-3">
          <LanguageToggle />
          <Button>Get started free</Button>
        </div>
      </nav>
    </header>
  );
}
