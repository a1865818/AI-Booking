import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";

const withNextIntl = createNextIntlPlugin("./i18n/request.ts");

const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5126";
const signalrUrl = process.env.NEXT_PUBLIC_SIGNALR_URL ?? apiUrl;
const isDev = process.env.NODE_ENV === "development";

function buildCsp() {
  const connectSrc = [
    "'self'",
    apiUrl,
    signalrUrl,
    "https://*.mux.com",
    "https://inferred.litix.io",
    ...(isDev ? ["ws:", "wss:"] : []),
  ].join(" ");

  return [
    "default-src 'self'",
    isDev
      ? "script-src 'self' 'unsafe-inline' 'unsafe-eval'"
      : "script-src 'self' 'unsafe-inline'",
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data: blob: https://image.mux.com",
    "media-src 'self' blob: https://*.mux.com",
    `connect-src ${connectSrc}`,
    "font-src 'self'",
    "frame-ancestors 'none'",
    "base-uri 'self'",
    "form-action 'self'",
  ].join("; ");
}

const nextConfig: NextConfig = {
  async headers() {
    const securityHeaders = [
      { key: "X-Content-Type-Options", value: "nosniff" },
      { key: "X-Frame-Options", value: "DENY" },
      { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
      {
        key: "Permissions-Policy",
        value: "camera=(), microphone=(), geolocation=()",
      },
      { key: "Content-Security-Policy", value: buildCsp() },
    ];

    if (!isDev) {
      securityHeaders.push({
        key: "Strict-Transport-Security",
        value: "max-age=31536000; includeSubDomains",
      });
    }

    return [{ source: "/:path*", headers: securityHeaders }];
  },
};

export default withNextIntl(nextConfig);
