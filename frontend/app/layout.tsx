import type { Metadata, Viewport } from "next";
import { Inter } from "next/font/google";
import { NextIntlClientProvider } from "next-intl";
import { getLocale, getMessages } from "next-intl/server";
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
  },
};

export const viewport: Viewport = {
  themeColor: "#0d0d0d",
};

export default async function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const locale = await getLocale();
  const messages = await getMessages();

  return (
    <html lang={locale} className={inter.variable} suppressHydrationWarning>
      <body>
        <NextIntlClientProvider locale={locale} messages={messages}>
          <ThemeProvider>{children}</ThemeProvider>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
