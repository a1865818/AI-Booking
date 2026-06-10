"use client";

import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { HubConnectionState, type HubConnection } from "@microsoft/signalr";
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
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  // Track the active connection so the cleanup can stop the right instance.
  const connRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    if (typeof window === "undefined") return;

    let disposed = false;

    // Fresh connection per effect run — a stopped HubConnection cannot be restarted.
    const conn = createBookingConnection(getAccessToken);
    connRef.current = conn;
    setConnection(conn);

    void (async () => {
      try {
        await conn.start();
        if (disposed) {
          await conn.stop();
          return;
        }
        setConnected(true);
      } catch {
        if (!disposed && connRef.current === conn) setConnected(false);
      }
    })();

    return () => {
      disposed = true;
      connRef.current = null;
      // stop() during negotiation logs "connection was stopped during negotiation" in dev
      // when React Strict Mode unmounts before the handshake finishes.
      if (conn.state !== HubConnectionState.Connecting) {
        void conn.stop();
      }
    };
  }, []);

  return (
    <RealtimeContext.Provider value={{ connection, connected }}>
      {children}
    </RealtimeContext.Provider>
  );
}

export function useRealtime() {
  return useContext(RealtimeContext);
}
