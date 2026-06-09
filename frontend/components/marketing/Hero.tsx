"use client";

import { motion } from "framer-motion";
import { Button } from "@/components/ui/Button";

const fadeUp = {
  hidden: { opacity: 0, y: 20 },
  show: (delay: number) => ({
    opacity: 1,
    y: 0,
    transition: { delay, duration: 0.5, ease: "easeOut" as const },
  }),
};

export function Hero() {
  return (
    <section className="mx-auto flex min-h-screen max-w-6xl flex-col items-center gap-12 px-6 pt-32 pb-16 md:flex-row md:pt-24">
      <div className="md:w-[55%]">
        <motion.h1
          variants={fadeUp}
          initial="hidden"
          animate="show"
          custom={0}
          className="text-4xl font-bold leading-[1.1] text-primary md:text-5xl"
        >
          Your AI receptionist.
          <br />
          Books every seat.
        </motion.h1>

        <motion.p
          variants={fadeUp}
          initial="hidden"
          animate="show"
          custom={0.15}
          className="mt-6 max-w-xl text-base leading-7 text-secondary"
        >
          Ghế Đầy answers your customers over SMS in English and Vietnamese, books
          the slot, takes the deposit, and fills your waitlist — for nail salons,
          restaurants, barbershops, spas and beyond.
        </motion.p>

        <motion.div
          variants={fadeUp}
          initial="hidden"
          animate="show"
          custom={0.3}
          className="mt-8 flex items-center gap-4"
        >
          <Button>Get started free</Button>
          <Button variant="secondary">Watch the demo</Button>
        </motion.div>
      </div>

      <motion.div
        variants={fadeUp}
        initial="hidden"
        animate="show"
        custom={0.45}
        className="w-full md:w-[45%]"
      >
        <div className="aspect-video w-full rounded-xl bg-surface shadow-[var(--shadow-lg)]">
          {/* Phase 6: Mux player — real SMS → booking → dashboard flow, muted + looped. */}
          <div className="flex h-full items-center justify-center text-sm text-tertiary">
            SMS → booking → dashboard demo
          </div>
        </div>
      </motion.div>
    </section>
  );
}
