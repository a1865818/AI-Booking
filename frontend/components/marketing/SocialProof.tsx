import { Card } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";

const quotes = [
  {
    quote: "It books appointments while I'm doing nails. No more missed texts.",
    name: "Amy N.",
    type: "Nail Salon",
  },
  {
    quote: "Party-size questions and deposits are handled before I even look.",
    name: "Minh T.",
    type: "Restaurant",
  },
  {
    quote: "Walk-ins fill the chair the moment someone cancels.",
    name: "Carlos R.",
    type: "Barbershop",
  },
];

export function SocialProof() {
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
