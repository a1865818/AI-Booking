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
