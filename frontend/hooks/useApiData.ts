"use client";

import { useEffect, useState } from "react";
import { apiFetch, ApiError } from "@/lib/api-client";
import { getAccessToken, refreshAccessToken } from "@/lib/auth";

type State<T> = { data: T | null; loading: boolean; error: string | null };

/**
 * Fetches a tenant-scoped API resource with the in-memory access token, transparently
 * refreshing once on a 401. Exposes loading / error so callers can render the right state.
 */
export function useApiData<T>(path: string | null): State<T> & { reload: () => void } {
  const [state, setState] = useState<State<T>>({ data: null, loading: true, error: null });
  const [nonce, setNonce] = useState(0);

  useEffect(() => {
    if (path === null) return;
    let cancelled = false;

    async function run() {
      setState((s) => ({ ...s, loading: true, error: null }));
      try {
        const data = await fetchWithRetry<T>(path!);
        if (!cancelled) setState({ data, loading: false, error: null });
      } catch (err) {
        const message = err instanceof ApiError ? `Request failed (${err.status})` : "Something went wrong";
        if (!cancelled) setState({ data: null, loading: false, error: message });
      }
    }

    void run();
    return () => {
      cancelled = true;
    };
  }, [path, nonce]);

  return { ...state, reload: () => setNonce((n) => n + 1) };
}

async function fetchWithRetry<T>(path: string): Promise<T> {
  try {
    return await apiFetch<T>(path, { accessToken: getAccessToken() ?? undefined });
  } catch (err) {
    if (err instanceof ApiError && err.status === 401 && (await refreshAccessToken())) {
      return apiFetch<T>(path, { accessToken: getAccessToken() ?? undefined });
    }
    throw err;
  }
}
