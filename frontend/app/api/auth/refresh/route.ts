import { NextRequest, NextResponse } from "next/server";
import { API_URL } from "@/lib/api-client";

/** Rotates tokens using the httpOnly refresh cookie (Phase 5). */
export async function POST(request: NextRequest) {
  const refreshToken = request.cookies.get("refresh_token")?.value;
  if (!refreshToken) {
    return NextResponse.json({ error: "No refresh token" }, { status: 401 });
  }

  const upstream = await fetch(`${API_URL}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!upstream.ok) {
    return NextResponse.json({ error: "Refresh failed" }, { status: 401 });
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
