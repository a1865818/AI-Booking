"use client";

import { useEffect, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { Sidebar } from "@/components/dashboard/Sidebar";
import { RealtimeProvider } from "@/components/shared/RealtimeProvider";
import { getAccessToken, refreshAccessToken } from "@/lib/auth";
import { useTranslations } from "next-intl";

type Status = "checking" | "ready" | "unauthenticated";

/**
 * Client auth guard for the dashboard. On mount it exchanges the refresh cookie for an access
 * token; SignalR and data fetching only start once authenticated, so no request races ahead
 * without a token. Failure redirects to /login.
 */
export function DashboardShell({ children }: { children: ReactNode }) {
  const router = useRouter();
  const t = useTranslations("dashboard");
  const [status, setStatus] = useState<Status>("checking");

  useEffect(() => {
    // If we already have an in-memory token (e.g. just logged in), no need to hit the
    // refresh endpoint — this also prevents a double-consume in React Strict Mode dev.
    if (getAccessToken()) {
      setStatus("ready");
      return;
    }

    let cancelled = false;
    refreshAccessToken().then((ok) => {
      if (cancelled) return;
      if (ok) {
        setStatus("ready");
      } else {
        setStatus("unauthenticated");
        router.replace("/login");
      }
    });
    return () => {
      cancelled = true;
    };
  }, [router]);

  if (status !== "ready") {
    return (
      <div className="flex h-screen items-center justify-center text-sm text-secondary">
        {status === "checking" ? t("loadingDashboard") : t("redirectLogin")}
      </div>
    );
  }

  return (
    <RealtimeProvider>
      <div className="flex">
        <Sidebar />
        <div className="flex-1 overflow-y-auto p-8">{children}</div>
      </div>
    </RealtimeProvider>
  );
}
