import { useTranslations } from "next-intl";
import { Hero } from "@/components/marketing/Hero";
import { SocialProof } from "@/components/marketing/SocialProof";
import { VerticalShowcase } from "@/components/marketing/VerticalShowcase";
import { FeatureSection } from "@/components/marketing/FeatureSection";
import { HowItWorks } from "@/components/marketing/HowItWorks";
import { Pricing } from "@/components/marketing/Pricing";
import { Faq } from "@/components/marketing/Faq";

export default function HomePage() {
  const t = useTranslations("features");

  return (
    <>
      <Hero />
      <SocialProof />
      <VerticalShowcase />
      <FeatureSection
        eyebrow={t("bilingualEyebrow")}
        title={t("bilingualTitle")}
        body={t("bilingualBody")}
      />
      <FeatureSection
        reversed
        eyebrow={t("liveEyebrow")}
        title={t("liveTitle")}
        body={t("liveBody")}
      />
      <FeatureSection
        eyebrow={t("depositEyebrow")}
        title={t("depositTitle")}
        body={t("depositBody")}
      />
      <FeatureSection
        reversed
        eyebrow={t("waitlistEyebrow")}
        title={t("waitlistTitle")}
        body={t("waitlistBody")}
      />
      <HowItWorks />
      <Pricing />
      <Faq />
    </>
  );
}
