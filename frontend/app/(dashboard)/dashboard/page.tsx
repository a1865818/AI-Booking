"use client";

import { useApiData } from "@/hooks/useApiData";
import { ResourceGrid, type ResourceCell } from "@/components/dashboard/ResourceGrid";
import { BookingCard } from "@/components/dashboard/BookingCard";
import { LoadingState, ErrorState, EmptyState } from "@/components/ui/States";
import { useLiveBookings } from "@/hooks/useRealtimeBookings";

type Resource = { id: string; name: string; capacity: number };
type Settings = { resourceLabelPlural: string };
type Booking = {
  id: string;
  customerName: string;
  offeringName: string | null;
  resourceId: string | null;
  resourceName: string | null;
  startTime: string;
  partySize: number | null;
  status: "PendingDeposit" | "Confirmed" | "Completed" | "Cancelled" | "NoShow";
};

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

export default function TodayPage() {
  const settings = useApiData<Settings>("/api/settings");
  const resources = useApiData<Resource[]>("/api/resources");
  const bookings = useApiData<Booking[]>("/api/bookings");
  const { pulseResourceId } = useLiveBookings(bookings.reload);

  if (settings.loading || resources.loading || bookings.loading) {
    return <LoadingState label="Loading today" />;
  }

  const error = settings.error ?? resources.error ?? bookings.error;
  if (error) {
    return <ErrorState message={error} onRetry={() => { resources.reload(); bookings.reload(); }} />;
  }

  const todays = bookings.data ?? [];
  const stateByResource = new Map<string, ResourceCell["state"]>();
  for (const b of todays) {
    if (!b.resourceId) continue;
    if (b.status === "Confirmed") stateByResource.set(b.resourceId, "confirmed");
    else if (b.status === "PendingDeposit" && stateByResource.get(b.resourceId) !== "confirmed") {
      stateByResource.set(b.resourceId, "pending");
    }
  }

  const cells: ResourceCell[] = (resources.data ?? []).map((r) => ({
    id: r.id,
    name: r.name,
    state: stateByResource.get(r.id) ?? "free",
  }));

  return (
    <div className="flex flex-col gap-10">
      <div>
        <h1 className="text-3xl font-bold text-primary">Today</h1>
        <p className="mt-1 text-sm text-secondary">Live bookings appear here the moment they happen.</p>
      </div>

      <ResourceGrid
        resourceLabelPlural={settings.data?.resourceLabelPlural ?? "Resources"}
        resources={cells}
        newestResourceId={pulseResourceId}
      />

      <section>
        <h2 className="text-lg font-semibold text-primary">Today&apos;s bookings</h2>
        <div className="mt-4 flex flex-col gap-3">
          {todays.length === 0 ? (
            <EmptyState title="No bookings yet today" hint="New SMS bookings will show up here automatically." />
          ) : (
            todays.map((b) => (
              <BookingCard
                key={b.id}
                time={formatTime(b.startTime)}
                customerName={b.customerName}
                detail={b.offeringName ?? (b.partySize ? `Party of ${b.partySize}` : b.resourceName ?? "Reservation")}
                status={b.status}
              />
            ))
          )}
        </div>
      </section>
    </div>
  );
}
