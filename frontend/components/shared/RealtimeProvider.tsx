"use client";

import {
  createContext,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from "react";
import type { HubConnection } from "@microsoft/signalr";
import { createBookingConnection } from "@/lib/signalr";
import { getAccessToken } from "@/lib/auth";

type RealtimeContextValue = {
  connection: HubConnection | null;
  connected: boolean;
};

const RealtimeContext = createContext<RealtimeContextValue>({
  connection: null,
  connected: false,
});

/**
 * Opens a single BookingHub connection for the dashboard and shares it via context. Events are
 * already scoped server-side to the owner's `business_{id}` group (PLAN §2.7).
 */
export function RealtimeProvider({ children }: { children: ReactNode }) {
  // SignalR uses dynamic require under the hood, so only build the connection on the client —
  // never during SSR/prerender (the initializer runs once, no network until start()).
  const [connection] = useState<HubConnection | null>(() =>
    typeof window === "undefined" ? null : createBookingConnection(getAccessToken),
  );
  const [connected, setConnected] = useState(false);

  useEffect(() => {
    if (!connection) return;
    let cancelled = false;
    connection
      .start()
      .then(() => !cancelled && setConnected(true))
      .catch(() => !cancelled && setConnected(false));

    return () => {
      cancelled = true;
      void connection.stop();
    };
  }, [connection]);

  return (
    <RealtimeContext.Provider value={{ connection, connected }}>
      {children}
    </RealtimeContext.Provider>
  );
}

export function useRealtime() {
  return useContext(RealtimeContext);
}
