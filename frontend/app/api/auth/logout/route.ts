import { NextResponse } from "next/server";

/** Clears the refresh cookie (Phase 5). */
export async function POST() {
  const response = NextResponse.json({ ok: true });
  response.cookies.set("refresh_token", "", {
    httpOnly: true,
    secure: true,
    sameSite: "strict",
    path: "/",
    maxAge: 0,
  });
  return response;
}
