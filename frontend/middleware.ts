import { NextRequest, NextResponse } from "next/server";

/**
 * Auth guard for the dashboard (Phase 5). On a request to a protected route without a refresh
 * cookie, redirect to /login. On a 401 from the API, the client hits /api/auth/refresh to
 * rotate tokens and retries (PLAN §2.6).
 */
export function middleware(request: NextRequest) {
  const hasSession = request.cookies.has("refresh_token");

  if (!hasSession) {
    // Phase 5: redirect to /login. Allowed through for now so the scaffold is browsable.
    return NextResponse.next();
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/dashboard/:path*", "/conversations/:path*", "/bookings/:path*", "/settings/:path*"],
};
