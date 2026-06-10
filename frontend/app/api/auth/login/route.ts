import { NextRequest, NextResponse } from "next/server";
import { API_URL } from "@/lib/api-client";

/**
 * Proxies login to the .NET API, then stores the returned refresh token as an httpOnly
 * Secure SameSite=Strict cookie and returns only the short-lived access token to the client
 * (PLAN §2.6). Wired in Phase 5.
 */
export async function POST(request: NextRequest) {
  const body = await request.json();

  const upstream = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  if (!upstream.ok) {
    const detail = await upstream.text().catch(() => "");
    console.error(`[auth/login] upstream ${upstream.status}: ${detail}`);
    return NextResponse.json({ error: "Invalid credentials" }, { status: upstream.status });
  }

  const data = (await upstream.json()) as {
    accessToken: string;
    refreshToken?: string;
  };

  const response = NextResponse.json({ accessToken: data.accessToken });

  if (data.refreshToken) {
    response.cookies.set("refresh_token", data.refreshToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: 60 * 60 * 24 * 7,
    });
  }

  return response;
}
