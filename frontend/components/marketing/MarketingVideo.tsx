"use client";

import MuxPlayer from "@mux/mux-player-react";
import { cn } from "@/lib/utils";
import { muxEnvKey } from "@/lib/mux-config";

type MarketingVideoProps = {
  playbackId?: string;
  placeholder: string;
  className?: string;
  aspectClassName?: string;
};

export function MarketingVideo({
  playbackId,
  placeholder,
  className,
  aspectClassName = "aspect-video",
}: MarketingVideoProps) {
  const id = playbackId?.trim();

  return (
    <div
      className={cn(
        "w-full overflow-hidden rounded-xl bg-surface shadow-[var(--shadow-lg)]",
        aspectClassName,
        className,
      )}
    >
      {id ? (
        <MuxPlayer
          playbackId={id}
          streamType="on-demand"
          muted
          loop
          autoPlay="muted"
          envKey={muxEnvKey}
          className="h-full w-full object-cover"
        />
      ) : (
        <div className="flex h-full min-h-48 items-center justify-center px-6 text-center text-sm text-tertiary">
          {placeholder}
        </div>
      )}
    </div>
  );
}
