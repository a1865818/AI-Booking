import { DashboardShell } from "@/components/dashboard/DashboardShell";

/**
 * Dashboard shell: client-side auth guard + SignalR provider + sidebar (PLAN §2.3).
 * The guard exchanges the httpOnly refresh cookie for an access token before rendering.
 */
export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <DashboardShell>{children}</DashboardShell>;
}
