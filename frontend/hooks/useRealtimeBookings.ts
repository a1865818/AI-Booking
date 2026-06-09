"use client";

import { useEffect, useState } from "react";
import { useRealtime } from "@/components/shared/RealtimeProvider";

export type Booking = {
  id: string;
  resourceId: string | null;
  startTime: string;
  endTime: string;
  partySize: number | null;
  status: string;
};

/**
 * Keeps a live list of bookings, prepending new ones pushed over SignalR. The marker
 * `isNew` drives the slide-in + emerald pulse animation (PLAN §3.6).
 */
export function useRealtimeBookings(initial: Booking[] = []) {
  const { connection } = useRealtime();
  const [bookings, setBookings] = useState<Booking[]>(initial);
  const [newest, setNewest] = useState<string | null>(null);

  useEffect(() => {
    if (!connection) return;

    const onCreated = (payload: { booking: Booking }) => {
      setBookings((prev) => [payload.booking, ...prev]);
      setNewest(payload.booking.id);
    };

    const onStatusChanged = (payload: { bookingId: string; newStatus: string }) => {
      setBookings((prev) =>
        prev.map((b) =>
          b.id === payload.bookingId ? { ...b, status: payload.newStatus } : b,
        ),
      );
    };

    connection.on("BookingCreated", onCreated);
    connection.on("BookingStatusChanged", onStatusChanged);

    return () => {
      connection.off("BookingCreated", onCreated);
      connection.off("BookingStatusChanged", onStatusChanged);
    };
  }, [connection]);

  return { bookings, newestBookingId: newest };
}
