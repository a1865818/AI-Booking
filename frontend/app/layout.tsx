import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import { ThemeProvider } from "@/components/shared/ThemeProvider";
import "./globals.css";

const inter = Inter({
  subsets: ["latin", "vietnamese"],
  variable: "--font-inter",
  display: "swap",
});

export const metadata: Metadata = {
  metadataBase: new URL(process.env.NEXT_PUBLIC_APP_URL ?? "http://localhost:3000"),
  title: "Ghế Đầy — Your AI receptionist. Books every seat.",
  description:
    "An AI assistant that handles customer bookings over SMS in English and Vietnamese, for any seat-based service business.",
  openGraph: {
    title: "Ghế Đầy — Your AI receptionist. Books every seat.",
    description:
      "Books every seat over SMS in English and Vietnamese — nail salons, restaurants, barbershops, spas and beyond.",
    type: "website",
    images: ["/og/og-default.png"],
  },
};

export const viewport: Viewport = {
  themeColor: "#0d0d0d",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" className={inter.variable} suppressHydrationWarning>
      <body>
        <ThemeProvider>{children}</ThemeProvider>
      </body>
    </html>
  );
}
