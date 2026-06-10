export const muxEnvKey = process.env.NEXT_PUBLIC_MUX_ENV_KEY?.trim() || undefined;

export const muxPlaybackIds = {
  hero: process.env.NEXT_PUBLIC_MUX_PLAYBACK_HERO?.trim() || undefined,
  nail: process.env.NEXT_PUBLIC_MUX_PLAYBACK_NAIL?.trim() || undefined,
  restaurant: process.env.NEXT_PUBLIC_MUX_PLAYBACK_RESTAURANT?.trim() || undefined,
  barber: process.env.NEXT_PUBLIC_MUX_PLAYBACK_BARBER?.trim() || undefined,
} as const;
