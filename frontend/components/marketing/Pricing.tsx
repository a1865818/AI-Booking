import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";

export function Pricing() {
  const t = useTranslations("pricing");

  const plans = [
    {
      name: t("starterName"),
      price: t("starterPrice"),
      features: t.raw("starterFeatures") as string[],
      featured: false,
    },
    {
      name: t("growthName"),
      price: t("growthPrice"),
      features: t.raw("growthFeatures") as string[],
      featured: true,
    },
  ];

  return (
    <section id="pricing" className="mx-auto max-w-5xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">{t("heading")}</h2>
      <div className="mt-12 grid gap-6 md:grid-cols-2">
        {plans.map((plan) => (
          <Card
            key={plan.name}
            className="flex flex-col gap-6 data-[featured=true]:ring-1 data-[featured=true]:ring-accent"
            data-featured={plan.featured}
          >
            <div className="flex items-center justify-between">
              <h3 className="text-xl font-semibold text-primary">{plan.name}</h3>
              {plan.featured && <Badge tone="accent">{t("mostPopular")}</Badge>}
            </div>
            <p className="flex items-baseline gap-1">
              <span className="text-4xl font-bold text-primary">{plan.price}</span>
              <span className="text-sm text-secondary">{t("perLocation")}</span>
            </p>
            <ul className="flex flex-col gap-2 text-sm text-secondary">
              {plan.features.map((f) => (
                <li key={f}>· {f}</li>
              ))}
            </ul>
            <Button variant={plan.featured ? "primary" : "secondary"} className="mt-auto">
              {t("getStarted")}
            </Button>
          </Card>
        ))}
      </div>
    </section>
  );
}
