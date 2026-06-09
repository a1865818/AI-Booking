"use client";

import { useCallback, useState } from "react";
import { getAccessToken, setAccessToken } from "@/lib/auth";

/**
 * Client auth surface (Phase 5). Login/refresh proxy through the Next.js route handlers so the
 * refresh token can be set as an httpOnly cookie; only the access token is held in memory.
 */
export function useAuth() {
  const [authenticated, setAuthenticated] = useState<boolean>(
    () => getAccessToken() !== null,
  );

  const login = useCallback(async (email: string, password: string) => {
    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) throw new Error("Login failed");
    const { accessToken } = (await res.json()) as { accessToken: string };
    setAccessToken(accessToken);
    setAuthenticated(true);
  }, []);

  const logout = useCallback(async () => {
    await fetch("/api/auth/logout", { method: "POST" });
    setAccessToken(null);
    setAuthenticated(false);
  }, []);

  return { authenticated, login, logout };
}
