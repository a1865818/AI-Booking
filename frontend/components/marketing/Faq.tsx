"use client";

import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

const faqs = [
  { q: "Does it really speak Vietnamese?", a: "Yes. Ghế Đầy replies in the language your customer texts in — English or Vietnamese — automatically." },
  { q: "What kinds of businesses does it work for?", a: "Any seat-based service: nail salons, restaurants, barbershops, spas, and more. Behaviour is configured per business, not hardcoded." },
  { q: "How are deposits handled?", a: "Through Stripe. You onboard once with Stripe Connect Express; customers pay via a hosted link sent over SMS." },
  { q: "What happens on a no-show?", a: "Bookings are auto-marked and your waitlist is offered the freed slot over SMS." },
  { q: "Do I need a new phone number?", a: "We provision a dedicated number per location so your existing line is untouched." },
  { q: "Can I take over a conversation?", a: "Anytime. Pause the AI on any conversation and reply manually from the dashboard." },
  { q: "Is there a contract?", a: "No. Monthly, per location, cancel anytime." },
  { q: "How fast can I go live?", a: "Once your offerings and hours are in and Stripe is connected, the first SMS books correctly immediately." },
];

export function Faq() {
  const [open, setOpen] = useState<number | null>(0);

  return (
    <section id="faq" className="mx-auto max-w-3xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">
        Frequently asked questions
      </h2>
      <div className="mt-10 flex flex-col divide-y divide-border-subtle">
        {faqs.map((faq, i) => (
          <div key={faq.q}>
            <button
              type="button"
              aria-expanded={open === i}
              onClick={() => setOpen(open === i ? null : i)}
              className="flex w-full items-center justify-between py-4 text-left"
            >
              <span className="font-semibold text-primary">{faq.q}</span>
              <ChevronDown
                className={cn(
                  "h-4 w-4 text-secondary transition-transform duration-150",
                  open === i && "rotate-180",
                )}
              />
            </button>
            <div
              className={cn(
                "grid overflow-hidden transition-all duration-250",
                open === i ? "grid-rows-[1fr] pb-4" : "grid-rows-[0fr]",
              )}
            >
              <p className="min-h-0 text-sm leading-6 text-secondary">{faq.a}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
