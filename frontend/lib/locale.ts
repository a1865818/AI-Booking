export const LOCALE_COOKIE = "NEXT_LOCALE";

/** Persists the chosen locale so the next-intl request config picks it up server-side. */
export function persistLocale(locale: string): void {
  document.cookie = `${LOCALE_COOKIE}=${locale};path=/;max-age=31536000;samesite=lax`;
}
