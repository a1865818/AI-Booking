import { useTranslations } from "next-intl";

export function HowItWorks() {
  const t = useTranslations("how");
  const steps = [
    { n: 1, title: t("step1Title"), body: t("step1Body") },
    { n: 2, title: t("step2Title"), body: t("step2Body") },
    { n: 3, title: t("step3Title"), body: t("step3Body") },
  ];

  return (
    <section id="how" className="mx-auto max-w-6xl px-6 py-24">
      <h2 className="text-center text-3xl font-semibold text-primary">{t("heading")}</h2>
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
