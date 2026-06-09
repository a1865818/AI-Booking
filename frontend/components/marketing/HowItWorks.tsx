const steps = [
  { n: 1, title: "Send a text", body: "Your customer texts your number like they text a friend." },
  { n: 2, title: "AI books + takes deposit", body: "Ghế Đầy confirms the slot and collects a deposit when needed." },
  { n: 3, title: "You focus on guests", body: "Every booking lands on your dashboard in real time." },
];

export function HowItWorks() {
  return (
    <section id="how" className="mx-auto max-w-6xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">
        How it works
      </h2>
      <ol className="mt-12 grid gap-8 md:grid-cols-3">
        {steps.map((s) => (
          <li key={s.n} className="flex flex-col items-center text-center">
            <span className="flex h-12 w-12 items-center justify-center rounded-pill bg-accent-subtle text-lg font-bold text-accent">
              {s.n}
            </span>
            <h3 className="mt-4 text-lg font-semibold text-primary">{s.title}</h3>
            <p className="mt-2 max-w-xs text-sm leading-6 text-secondary">{s.body}</p>
          </li>
        ))}
      </ol>
    </section>
  );
}
