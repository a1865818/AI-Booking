export const locales = ["en", "vi"] as const;
export type Locale = (typeof locales)[number];
export const defaultLocale: Locale = "en";

/**
 * Minimal copy dictionary for marketing + dashboard chrome. Phase 6 swaps this for
 * `next-intl` message catalogs; the shape stays the same so callers don't change.
 */
export const messages: Record<Locale, Record<string, string>> = {
  en: {
    "nav.features": "Features",
    "nav.pricing": "Pricing",
    "nav.faq": "FAQ",
    "cta.getStarted": "Get started free",
    "hero.headline": "Your AI receptionist. Books every seat.",
    "hero.subtitle":
      "Ghế Đầy answers your customers over SMS in English and Vietnamese, books the slot, takes the deposit, and fills your waitlist — for any seat-based business.",
  },
  vi: {
    "nav.features": "Tính năng",
    "nav.pricing": "Bảng giá",
    "nav.faq": "Câu hỏi",
    "cta.getStarted": "Bắt đầu miễn phí",
    "hero.headline": "Lễ tân AI của bạn. Lấp đầy mọi chỗ ngồi.",
    "hero.subtitle":
      "Ghế Đầy trả lời khách qua SMS bằng tiếng Anh và tiếng Việt, đặt chỗ, nhận tiền cọc và lấp đầy danh sách chờ — cho mọi cơ sở kinh doanh theo chỗ ngồi.",
  },
};

export function t(locale: Locale, key: string): string {
  return messages[locale]?.[key] ?? messages[defaultLocale][key] ?? key;
}
