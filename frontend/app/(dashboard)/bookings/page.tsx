"use client";

import { useApiData } from "@/hooks/useApiData";
import { BookingCard } from "@/components/dashboard/BookingCard";
import { LoadingState, ErrorState, EmptyState } from "@/components/ui/States";

type Booking = {
  id: string;
  customerName: string;
  offeringName: string | null;
  resourceName: string | null;
  startTime: string;
  partySize: number | null;
  status: "PendingDeposit" | "Confirmed" | "Completed" | "Cancelled" | "NoShow";
};

function formatWhen(iso: string) {
  return new Date(iso).toLocaleString([], {
    weekday: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function BookingsPage() {
  const { data, loading, error, reload } = useApiData<Booking[]>("/api/bookings");

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-bold text-primary">Bookings</h1>

      {loading && <LoadingState label="Loading bookings" />}
      {error && <ErrorState message={error} onRetry={reload} />}
      {!loading && !error && (data?.length ?? 0) === 0 && (
        <EmptyState title="No bookings for today" hint="Bookings made over SMS appear here." />
      )}

      {!loading && !error && (data?.length ?? 0) > 0 && (
        <div className="flex flex-col gap-3">
          {data!.map((b) => (
            <BookingCard
              key={b.id}
              time={formatWhen(b.startTime)}
              customerName={b.customerName}
              detail={b.offeringName ?? (b.partySize ? `Party of ${b.partySize}` : b.resourceName ?? "Reservation")}
              status={b.status}
            />
          ))}
        </div>
      )}
    </div>
  );
}
