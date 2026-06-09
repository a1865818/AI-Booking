import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";

const plans = [
  {
    name: "Starter",
    price: "$49",
    cadence: "/ location / mo",
    features: [
      "1 location",
      "Unlimited SMS bookings",
      "Deposits via Stripe",
      "Waitlist auto-fill",
      "EN + VI",
    ],
    featured: false,
  },
  {
    name: "Growth",
    price: "$129",
    cadence: "/ location / mo",
    features: [
      "Multi-location dashboard",
      "Everything in Starter",
      "Priority SMS throughput",
      "Custom AI persona",
      "Reminders + no-show recovery",
    ],
    featured: true,
  },
];

export function Pricing() {
  return (
    <section id="pricing" className="mx-auto max-w-5xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">
        Simple pricing, per location
      </h2>
      <div className="mt-12 grid gap-6 md:grid-cols-2">
        {plans.map((plan) => (
          <Card
            key={plan.name}
            className="flex flex-col gap-6 data-[featured=true]:ring-1 data-[featured=true]:ring-accent"
            data-featured={plan.featured}
          >
            <div className="flex items-center justify-between">
              <h3 className="text-xl font-semibold text-primary">{plan.name}</h3>
              {plan.featured && <Badge tone="accent">Most popular</Badge>}
            </div>
            <p className="flex items-baseline gap-1">
              <span className="text-4xl font-bold text-primary">{plan.price}</span>
              <span className="text-sm text-secondary">{plan.cadence}</span>
            </p>
            <ul className="flex flex-col gap-2 text-sm text-secondary">
              {plan.features.map((f) => (
                <li key={f}>· {f}</li>
              ))}
            </ul>
            <Button variant={plan.featured ? "primary" : "secondary"} className="mt-auto">
              Get started free
            </Button>
          </Card>
        ))}
      </div>
    </section>
  );
}
