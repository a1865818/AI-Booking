"use client";

import { useCallback, useEffect, useState } from "react";
import { useRealtime } from "@/components/shared/RealtimeProvider";

type BookingCreatedPayload = {
  booking: { id: string; resourceId?: string | null };
};

/**
 * Subscribes to BookingHub events and triggers a data reload so lists stay in sync.
 * Returns the resource id to pulse when a new booking lands (PLAN §3.6).
 */
export function useLiveBookings(reload: () => void) {
  const { connection, connected } = useRealtime();
  const [pulseResourceId, setPulseResourceId] = useState<string | null>(null);

  const stableReload = useCallback(() => reload(), [reload]);

  useEffect(() => {
    if (!connection || !connected) return;

    const onCreated = (payload: BookingCreatedPayload) => {
      stableReload();
      if (payload.booking.resourceId) setPulseResourceId(payload.booking.resourceId);
    };

    const onStatusChanged = () => stableReload();

    connection.on("BookingCreated", onCreated);
    connection.on("BookingStatusChanged", onStatusChanged);

    return () => {
      connection.off("BookingCreated", onCreated);
      connection.off("BookingStatusChanged", onStatusChanged);
    };
  }, [connection, connected, stableReload]);

  return { pulseResourceId };
}

export type Booking = {
  id: string;
  resourceId: string | null;
  startTime: string;
  endTime: string;
  partySize: number | null;
  status: string;
};

/** @deprecated Use useLiveBookings for dashboard pages that fetch via useApiData. */
export function useRealtimeBookings(initial: Booking[] = []) {
  const { connection } = useRealtime();
  const [bookings, setBookings] = useState<Booking[]>(initial);
  const [newest, setNewest] = useState<string | null>(null);

  useEffect(() => {
    if (!connection) return;

    const onCreated = (payload: { booking: Booking }) => {
      setBookings((prev) => [payload.booking, ...prev]);
      setNewest(payload.booking.resourceId ?? payload.booking.id);
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
