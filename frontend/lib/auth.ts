/**
 * Client-side access-token holder. The long-lived refresh token lives in an httpOnly cookie
 * set by the Next.js auth route handlers; only the short-lived access token is kept in memory
 * (Phase 5).
 */
let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

/**
 * Exchanges the httpOnly refresh cookie for a fresh access token via the Next.js BFF route.
 * Returns true when an access token was obtained.
 */
export async function refreshAccessToken(): Promise<boolean> {
  try {
    const res = await fetch("/api/auth/refresh", { method: "POST" });
    if (!res.ok) return false;
    const { accessToken: token } = (await res.json()) as { accessToken: string };
    setAccessToken(token);
    return true;
  } catch {
    return false;
  }
}
