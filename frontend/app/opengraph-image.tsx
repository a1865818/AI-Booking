import { ImageResponse } from "next/og";

export const alt = "Ghế Đầy — Your AI receptionist. Books every seat.";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OpengraphImage() {
  return new ImageResponse(
    (
      <div
        style={{
          width: "100%",
          height: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          padding: "80px",
          background: "#0d0d0d",
          color: "#f0f0f0",
          fontFamily: "sans-serif",
        }}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 20, marginBottom: 40 }}>
          <div style={{ width: 56, height: 56, borderRadius: 9999, background: "#10b981" }} />
          <div style={{ fontSize: 36, fontWeight: 700 }}>Ghế Đầy</div>
        </div>
        <div style={{ fontSize: 72, fontWeight: 700, lineHeight: 1.1, maxWidth: 900 }}>
          Your AI receptionist. Books every seat.
        </div>
        <div style={{ fontSize: 30, color: "#888888", marginTop: 28, maxWidth: 900 }}>
          Bookings over SMS in English & Vietnamese — for any seat-based business.
        </div>
      </div>
    ),
    size,
  );
}
