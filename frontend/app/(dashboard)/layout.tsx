import { Sidebar } from "@/components/dashboard/Sidebar";
import { RealtimeProvider } from "@/components/shared/RealtimeProvider";

/**
 * Dashboard shell: auth guard + SignalR provider + sidebar (PLAN §2.3).
 *
 * Auth guard (Phase 5): middleware.ts redirects unauthenticated requests to /login before
 * this layout renders; the access token is refreshed on 401 via /api/auth/refresh.
 */
export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <RealtimeProvider>
      <div className="flex">
        <Sidebar />
        <div className="flex-1 overflow-y-auto p-8">{children}</div>
      </div>
    </RealtimeProvider>
  );
}
