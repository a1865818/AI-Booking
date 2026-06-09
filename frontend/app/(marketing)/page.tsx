import { Hero } from "@/components/marketing/Hero";
import { SocialProof } from "@/components/marketing/SocialProof";
import { VerticalShowcase } from "@/components/marketing/VerticalShowcase";
import { FeatureSection } from "@/components/marketing/FeatureSection";
import { HowItWorks } from "@/components/marketing/HowItWorks";
import { Pricing } from "@/components/marketing/Pricing";
import { Faq } from "@/components/marketing/Faq";

export default function HomePage() {
  return (
    <>
      <Hero />
      <SocialProof />
      <VerticalShowcase />
      <FeatureSection
        eyebrow="Bilingual"
        title="Speaks Vietnamese"
        body="Your customers text the way they speak. Ghế Đầy replies in their language and books without a hitch."
      />
      <FeatureSection
        reversed
        eyebrow="Live"
        title="See every booking, live"
        body="New bookings slide onto your dashboard the instant they happen — no refresh, no app to babysit."
      />
      <FeatureSection
        eyebrow="Deposits"
        title="Deposits end no-shows"
        body="Send a Stripe payment link over SMS for the bookings that need it, and watch no-shows disappear."
      />
      <FeatureSection
        reversed
        eyebrow="Waitlist"
        title="Waitlist fills every gap"
        body="When a slot opens, the next customer in line is offered it automatically over SMS."
      />
      <HowItWorks />
      <Pricing />
      <Faq />
    </>
  );
}
