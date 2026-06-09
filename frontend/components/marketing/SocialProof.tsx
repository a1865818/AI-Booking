import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";

export function SocialProof() {
  const t = useTranslations("social");
  const quotes = [
    { quote: t("quote1"), name: t("name1"), type: t("typeNail") },
    { quote: t("quote2"), name: t("name2"), type: t("typeRestaurant") },
    { quote: t("quote3"), name: t("name3"), type: t("typeBarber") },
  ];

  return (
    <section className="mx-auto max-w-6xl px-6 py-20">
      <div className="grid gap-6 md:grid-cols-3">
        {quotes.map((q) => (
          <Card key={q.name} className="flex flex-col gap-4">
            <Badge tone="accent">{q.type}</Badge>
            <p className="text-base leading-7 text-primary">“{q.quote}”</p>
            <p className="mt-auto text-sm text-secondary">{q.name}</p>
          </Card>
        ))}
      </div>
    </section>
  );
}
