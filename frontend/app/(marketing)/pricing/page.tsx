import type { Metadata } from "next";
import { Pricing } from "@/components/marketing/Pricing";
import { Faq } from "@/components/marketing/Faq";

export const metadata: Metadata = {
  title: "Pricing — Ghế Đầy",
  description: "Simple monthly pricing, per location. Cancel anytime.",
};

export default function PricingPage() {
  return (
    <div className="pt-16">
      <Pricing />
      <Faq />
    </div>
  );
}
