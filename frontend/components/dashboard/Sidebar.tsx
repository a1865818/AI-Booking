"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { CalendarDays, MessagesSquare, ListChecks, Settings } from "lucide-react";
import { cn } from "@/lib/utils";

const nav = [
  { href: "/dashboard", label: "Today", icon: CalendarDays },
  { href: "/conversations", label: "Conversations", icon: MessagesSquare },
  { href: "/bookings", label: "Bookings", icon: ListChecks },
  { href: "/settings", label: "Settings", icon: Settings },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="flex h-screen w-60 flex-col gap-2 border-r border-border-subtle bg-surface p-4">
      <div className="mb-4 flex items-center gap-2 px-2 font-bold text-primary">
        <span aria-hidden className="inline-block h-6 w-6 rounded-pill bg-accent" />
        Ghế Đầy
      </div>
      <nav className="flex flex-col gap-1">
        {nav.map((item) => {
          const active = pathname.startsWith(item.href);
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors",
                active
                  ? "bg-elevated font-bold text-primary"
                  : "text-secondary hover:bg-elevated hover:text-primary",
              )}
            >
              <Icon className="h-4 w-4" />
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}
