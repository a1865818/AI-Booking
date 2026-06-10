"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/Button";
import { apiFetch } from "@/lib/api-client";
import { getAccessToken } from "@/lib/auth";

const inputClass =
  "w-full rounded-md bg-elevated px-3 py-2 text-sm text-primary shadow-[var(--shadow-inset)] placeholder:text-tertiary";

export type SettingsBundle = {
  name: string;
  businessType: string;
  timezone: string;
  resourceLabelPlural: string;
  verticalConfig?: {
    depositRequired?: boolean;
    depositCents?: number;
    holdMinutes?: number;
    depositThresholdPartySize?: number;
    depositPerHeadCents?: number;
  };
  settings?: { aiPersona?: string | null };
  hours?: { dayOfWeek: number; open: string; close: string }[];
  stripeConnected?: boolean;
};

export type OfferingRow = {
  id: string;
  name: string;
  nameVi: string | null;
  durationMinutes: number;
  priceCents: number;
  isResourceOnly: boolean;
  isActive: boolean;
};

export type ResourceRow = {
  id: string;
  name: string;
  capacity: number;
  isActive: boolean;
};

type Tab = "offerings" | "resources" | "hours" | "deposit" | "persona" | "stripe";

type SaveState = "idle" | "saving" | "saved" | "error";

function defaultHours(): { dayOfWeek: number; open: string; close: string }[] {
  return Array.from({ length: 7 }, (_, dayOfWeek) => ({
    dayOfWeek,
    open: "09:00",
    close: "17:00",
  }));
}

function normalizeSettings(data: SettingsBundle): SettingsBundle {
  return {
    ...data,
    resourceLabelPlural: data.resourceLabelPlural ?? "Resources",
    verticalConfig: data.verticalConfig ?? {},
    settings: data.settings ?? {},
    hours: Array.isArray(data.hours) ? data.hours : [],
    stripeConnected: data.stripeConnected ?? false,
  };
}

function resolveHours(hours: SettingsBundle["hours"] | undefined) {
  const list = Array.isArray(hours) ? hours : [];
  return list.length > 0 ? list : defaultHours();
}

export function SettingsForm({
  settings: rawSettings,
  offerings: initialOfferings,
  resources: initialResources,
  onSettingsSaved,
  onOfferingsChanged,
  onResourcesChanged,
}: {
  settings: SettingsBundle;
  offerings: OfferingRow[];
  resources: ResourceRow[];
  onSettingsSaved: () => void;
  onOfferingsChanged: () => void;
  onResourcesChanged: () => void;
}) {
  const settings = normalizeSettings(rawSettings);
  const t = useTranslations("dashboard");
  const [active, setActive] = useState<Tab>("offerings");
  const [saveState, setSaveState] = useState<SaveState>("idle");

  const [offerings, setOfferings] = useState(initialOfferings ?? []);
  const [resources, setResources] = useState(initialResources ?? []);
  const [hours, setHours] = useState(() => resolveHours(settings.hours));
  const [persona, setPersona] = useState(settings.settings?.aiPersona ?? "");
  const [depositCents, setDepositCents] = useState(
    ((settings.verticalConfig?.depositCents ?? 0) / 100).toString(),
  );
  const [holdMinutes, setHoldMinutes] = useState(
    String(settings.verticalConfig?.holdMinutes ?? 15),
  );
  const [depositThreshold, setDepositThreshold] = useState(
    String(settings.verticalConfig?.depositThresholdPartySize ?? 6),
  );
  const [depositPerHead, setDepositPerHead] = useState(
    ((settings.verticalConfig?.depositPerHeadCents ?? 0) / 100).toString(),
  );

  const [newOffering, setNewOffering] = useState({
    name: "",
    nameVi: "",
    durationMinutes: "60",
    priceDollars: "0",
  });

  const [newResource, setNewResource] = useState({
    name: "",
    capacity: "1",
  });

  const [connecting, setConnecting] = useState(false);
  const [connectError, setConnectError] = useState<string | null>(null);

  useEffect(() => setOfferings(initialOfferings ?? []), [initialOfferings]);
  useEffect(() => setResources(initialResources ?? []), [initialResources]);
  useEffect(() => {
    const next = normalizeSettings(rawSettings);
    setHours(resolveHours(next.hours));
    setPersona(next.settings?.aiPersona ?? "");
    setDepositCents(((next.verticalConfig?.depositCents ?? 0) / 100).toString());
    setHoldMinutes(String(next.verticalConfig?.holdMinutes ?? 15));
    setDepositThreshold(String(next.verticalConfig?.depositThresholdPartySize ?? 6));
    setDepositPerHead(((next.verticalConfig?.depositPerHeadCents ?? 0) / 100).toString());
  }, [rawSettings]);

  const token = () => getAccessToken() ?? undefined;
  const dayLabels = (t.raw("days") as string[] | undefined) ?? [
    "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday",
  ];

  async function withSave(run: () => Promise<void>) {
    setSaveState("saving");
    try {
      await run();
      setSaveState("saved");
      setTimeout(() => setSaveState("idle"), 2000);
    } catch {
      setSaveState("error");
    }
  }

  async function saveOffering(row: OfferingRow) {
    await withSave(async () => {
      await apiFetch(`/api/offerings/${row.id}`, {
        method: "PUT",
        accessToken: token(),
        body: JSON.stringify({
          name: row.name,
          nameVi: row.nameVi,
          durationMinutes: row.durationMinutes,
          priceCents: row.priceCents,
          isResourceOnly: row.isResourceOnly,
          isActive: row.isActive,
        }),
      });
      onOfferingsChanged();
    });
  }

  async function addOffering() {
    if (!newOffering.name.trim()) return;
    await withSave(async () => {
      const created = await apiFetch<OfferingRow>("/api/offerings", {
        method: "POST",
        accessToken: token(),
        body: JSON.stringify({
          name: newOffering.name,
          nameVi: newOffering.nameVi || null,
          durationMinutes: Number(newOffering.durationMinutes) || 60,
          priceCents: Math.round(Number(newOffering.priceDollars) * 100) || 0,
          isResourceOnly: false,
        }),
      });
      setOfferings((prev) => [...prev, created]);
      setNewOffering({ name: "", nameVi: "", durationMinutes: "60", priceDollars: "0" });
      onOfferingsChanged();
    });
  }

  async function deactivateOffering(row: OfferingRow) {
    const updated = { ...row, isActive: false };
    setOfferings((prev) => prev.map((o) => (o.id === row.id ? updated : o)));
    await withSave(async () => {
      await apiFetch(`/api/offerings/${row.id}`, {
        method: "PUT",
        accessToken: token(),
        body: JSON.stringify({
          name: row.name,
          nameVi: row.nameVi,
          durationMinutes: row.durationMinutes,
          priceCents: row.priceCents,
          isResourceOnly: row.isResourceOnly,
          isActive: false,
        }),
      });
      onOfferingsChanged();
    });
  }

  async function saveResource(row: ResourceRow) {
    await withSave(async () => {
      await apiFetch(`/api/resources/${row.id}`, {
        method: "PUT",
        accessToken: token(),
        body: JSON.stringify({
          name: row.name,
          capacity: row.capacity,
          isActive: row.isActive,
        }),
      });
      onResourcesChanged();
    });
  }

  async function addResource() {
    if (!newResource.name.trim()) return;
    await withSave(async () => {
      const created = await apiFetch<ResourceRow & { resourceType?: string; sortOrder?: number }>(
        "/api/resources",
        {
          method: "POST",
          accessToken: token(),
          body: JSON.stringify({
            name: newResource.name,
            capacity: Number(newResource.capacity) || 1,
          }),
        },
      );
      setResources((prev) => [
        ...prev,
        {
          id: created.id,
          name: created.name,
          capacity: created.capacity,
          isActive: created.isActive,
        },
      ]);
      setNewResource({ name: "", capacity: "1" });
      onResourcesChanged();
    });
  }

  async function saveHours() {
    await withSave(async () => {
      await apiFetch("/api/settings/hours", {
        method: "PUT",
        accessToken: token(),
        body: JSON.stringify({ hours }),
      });
      onSettingsSaved();
    });
  }

  async function saveDeposit() {
    await withSave(async () => {
      await apiFetch("/api/settings/deposit", {
        method: "PUT",
        accessToken: token(),
        body: JSON.stringify({
          depositCents: Math.round(Number(depositCents) * 100),
          holdMinutes: Number(holdMinutes) || 15,
          depositThresholdPartySize: Number(depositThreshold) || null,
          depositPerHeadCents: Math.round(Number(depositPerHead) * 100) || null,
        }),
      });
      onSettingsSaved();
    });
  }

  async function savePersona() {
    await withSave(async () => {
      await apiFetch("/api/settings/persona", {
        method: "PUT",
        accessToken: token(),
        body: JSON.stringify({ aiPersona: persona }),
      });
      onSettingsSaved();
    });
  }

  async function startStripeConnect() {
    setConnecting(true);
    setConnectError(null);
    try {
      const { onboardingUrl } = await apiFetch<{ onboardingUrl: string }>(
        "/api/settings/stripe-connect",
        { method: "POST", accessToken: token() },
      );
      window.location.href = onboardingUrl;
    } catch {
      setConnectError(t("stripeUnavailable"));
    } finally {
      setConnecting(false);
    }
  }

  const tabs: { id: Tab; label: string }[] = [
    { id: "offerings", label: t("settingsTabOfferings") },
    { id: "resources", label: settings.resourceLabelPlural },
    { id: "hours", label: t("settingsTabHours") },
    { id: "deposit", label: t("settingsTabDeposit") },
    { id: "persona", label: t("settingsTabPersona") },
    { id: "stripe", label: t("settingsTabStripe") },
  ];

  const saveLabel =
    saveState === "saving" ? t("saving") : saveState === "saved" ? t("saved") : t("save");

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap gap-2">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            type="button"
            aria-pressed={active === tab.id}
            onClick={() => setActive(tab.id)}
            className={cn(
              "rounded-pill px-4 py-1.5 text-sm transition-colors",
              active === tab.id
                ? "bg-accent text-accent-on"
                : "border border-border-default text-secondary hover:bg-elevated",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {saveState === "error" && (
        <p role="alert" className="text-sm text-error">
          {t("saveFailed")}
        </p>
      )}

      <div className="rounded-lg bg-surface p-6">
        {active === "offerings" && (
          <div className="flex flex-col gap-6">
            {offerings.map((row) => (
              <div
                key={row.id}
                className={cn(
                  "grid gap-3 border-b border-border-subtle pb-4 last:border-0 md:grid-cols-5",
                  !row.isActive && "opacity-60",
                )}
              >
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("offeringName")}
                  <input
                    className={inputClass}
                    value={row.name}
                    onChange={(e) =>
                      setOfferings((prev) =>
                        prev.map((o) => (o.id === row.id ? { ...o, name: e.target.value } : o)),
                      )
                    }
                  />
                </label>
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("offeringNameVi")}
                  <input
                    className={inputClass}
                    value={row.nameVi ?? ""}
                    onChange={(e) =>
                      setOfferings((prev) =>
                        prev.map((o) => (o.id === row.id ? { ...o, nameVi: e.target.value } : o)),
                      )
                    }
                  />
                </label>
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("durationMin")}
                  <input
                    type="number"
                    min={1}
                    className={inputClass}
                    value={row.durationMinutes}
                    onChange={(e) =>
                      setOfferings((prev) =>
                        prev.map((o) =>
                          o.id === row.id
                            ? { ...o, durationMinutes: Number(e.target.value) || 60 }
                            : o,
                        ),
                      )
                    }
                  />
                </label>
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("priceDollars")}
                  <input
                    type="number"
                    min={0}
                    step={0.01}
                    className={inputClass}
                    value={(row.priceCents / 100).toFixed(2)}
                    onChange={(e) =>
                      setOfferings((prev) =>
                        prev.map((o) =>
                          o.id === row.id
                            ? { ...o, priceCents: Math.round(Number(e.target.value) * 100) }
                            : o,
                        ),
                      )
                    }
                  />
                </label>
                <div className="flex items-end gap-3">
                  <label className="flex items-center gap-2 text-sm text-secondary">
                    <input
                      type="checkbox"
                      checked={row.isActive}
                      onChange={(e) =>
                        setOfferings((prev) =>
                          prev.map((o) =>
                            o.id === row.id ? { ...o, isActive: e.target.checked } : o,
                          ),
                        )
                      }
                    />
                    {t("active")}
                  </label>
                  <Button type="button" onClick={() => void saveOffering(row)} disabled={saveState === "saving"}>
                    {saveLabel}
                  </Button>
                  {row.isActive && (
                    <Button
                      type="button"
                      variant="secondary"
                      onClick={() => void deactivateOffering(row)}
                      disabled={saveState === "saving"}
                    >
                      {t("deactivateOffering")}
                    </Button>
                  )}
                </div>
              </div>
            ))}

            <div className="rounded-lg bg-elevated p-4">
              <p className="mb-3 text-sm font-semibold text-primary">{t("addOffering")}</p>
              <div className="grid gap-3 md:grid-cols-4">
                <input
                  className={inputClass}
                  placeholder={t("offeringName")}
                  value={newOffering.name}
                  onChange={(e) => setNewOffering((s) => ({ ...s, name: e.target.value }))}
                />
                <input
                  className={inputClass}
                  placeholder={t("offeringNameVi")}
                  value={newOffering.nameVi}
                  onChange={(e) => setNewOffering((s) => ({ ...s, nameVi: e.target.value }))}
                />
                <input
                  type="number"
                  className={inputClass}
                  placeholder={t("durationMin")}
                  value={newOffering.durationMinutes}
                  onChange={(e) =>
                    setNewOffering((s) => ({ ...s, durationMinutes: e.target.value }))
                  }
                />
                <input
                  type="number"
                  className={inputClass}
                  placeholder={t("priceDollars")}
                  value={newOffering.priceDollars}
                  onChange={(e) => setNewOffering((s) => ({ ...s, priceDollars: e.target.value }))}
                />
              </div>
              <Button className="mt-3" type="button" onClick={() => void addOffering()} disabled={saveState === "saving"}>
                {t("addOffering")}
              </Button>
            </div>
          </div>
        )}

        {active === "resources" && (
          <div className="flex flex-col gap-4">
            {resources.map((row) => (
              <div key={row.id} className="grid gap-3 md:grid-cols-4">
                <label className="flex flex-col gap-1 text-xs text-secondary md:col-span-2">
                  {t("resourceName")}
                  <input
                    className={inputClass}
                    value={row.name}
                    onChange={(e) =>
                      setResources((prev) =>
                        prev.map((r) => (r.id === row.id ? { ...r, name: e.target.value } : r)),
                      )
                    }
                  />
                </label>
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("capacity")}
                  <input
                    type="number"
                    min={1}
                    className={inputClass}
                    value={row.capacity}
                    onChange={(e) =>
                      setResources((prev) =>
                        prev.map((r) =>
                          r.id === row.id ? { ...r, capacity: Number(e.target.value) || 1 } : r,
                        ),
                      )
                    }
                  />
                </label>
                <div className="flex items-end gap-3">
                  <label className="flex items-center gap-2 text-sm text-secondary">
                    <input
                      type="checkbox"
                      checked={row.isActive}
                      onChange={(e) =>
                        setResources((prev) =>
                          prev.map((r) =>
                            r.id === row.id ? { ...r, isActive: e.target.checked } : r,
                          ),
                        )
                      }
                    />
                    {t("active")}
                  </label>
                  <Button type="button" onClick={() => void saveResource(row)} disabled={saveState === "saving"}>
                    {saveLabel}
                  </Button>
                </div>
              </div>
            ))}

            <div className="rounded-lg bg-elevated p-4">
              <p className="mb-3 text-sm font-semibold text-primary">{t("addResource")}</p>
              <div className="grid gap-3 md:grid-cols-3">
                <input
                  className={inputClass}
                  placeholder={t("resourceName")}
                  value={newResource.name}
                  onChange={(e) => setNewResource((s) => ({ ...s, name: e.target.value }))}
                />
                <input
                  type="number"
                  min={1}
                  className={inputClass}
                  placeholder={t("capacity")}
                  value={newResource.capacity}
                  onChange={(e) => setNewResource((s) => ({ ...s, capacity: e.target.value }))}
                />
              </div>
              <Button className="mt-3" type="button" onClick={() => void addResource()} disabled={saveState === "saving"}>
                {t("addResource")}
              </Button>
            </div>
          </div>
        )}

        {active === "hours" && (
          <div className="flex flex-col gap-4">
            {hours.map((row, idx) => (
              <div key={row.dayOfWeek} className="grid grid-cols-3 gap-3">
                <span className="self-center text-sm text-primary">
                  {dayLabels[row.dayOfWeek] ?? row.dayOfWeek}
                </span>
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("open")}
                  <input
                    type="time"
                    className={inputClass}
                    value={row.open}
                    onChange={(e) =>
                      setHours((prev) =>
                        prev.map((h, i) => (i === idx ? { ...h, open: e.target.value } : h)),
                      )
                    }
                  />
                </label>
                <label className="flex flex-col gap-1 text-xs text-secondary">
                  {t("close")}
                  <input
                    type="time"
                    className={inputClass}
                    value={row.close}
                    onChange={(e) =>
                      setHours((prev) =>
                        prev.map((h, i) => (i === idx ? { ...h, close: e.target.value } : h)),
                      )
                    }
                  />
                </label>
              </div>
            ))}
            <Button type="button" onClick={() => void saveHours()} disabled={saveState === "saving"}>
              {saveLabel}
            </Button>
          </div>
        )}

        {active === "deposit" && (
          <div className="flex max-w-md flex-col gap-4">
            {settings.verticalConfig?.depositRequired !== false && (
              <label className="flex flex-col gap-1 text-sm text-secondary">
                {t("depositCents")}
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  className={inputClass}
                  value={depositCents}
                  onChange={(e) => setDepositCents(e.target.value)}
                />
              </label>
            )}
            <label className="flex flex-col gap-1 text-sm text-secondary">
              {t("holdMinutes")}
              <input
                type="number"
                min={5}
                className={inputClass}
                value={holdMinutes}
                onChange={(e) => setHoldMinutes(e.target.value)}
              />
            </label>
            {settings.businessType === "Restaurant" && (
              <>
                <label className="flex flex-col gap-1 text-sm text-secondary">
                  {t("depositThreshold")}
                  <input
                    type="number"
                    min={1}
                    className={inputClass}
                    value={depositThreshold}
                    onChange={(e) => setDepositThreshold(e.target.value)}
                  />
                </label>
                <label className="flex flex-col gap-1 text-sm text-secondary">
                  {t("depositPerHead")}
                  <input
                    type="number"
                    min={0}
                    step={0.01}
                    className={inputClass}
                    value={depositPerHead}
                    onChange={(e) => setDepositPerHead(e.target.value)}
                  />
                </label>
              </>
            )}
            <Button type="button" onClick={() => void saveDeposit()} disabled={saveState === "saving"}>
              {saveLabel}
            </Button>
          </div>
        )}

        {active === "persona" && (
          <div className="flex max-w-xl flex-col gap-3">
            <p className="text-sm text-secondary">{t("personaHint")}</p>
            <textarea
              rows={5}
              className={cn(inputClass, "resize-y")}
              placeholder={t("personaPlaceholder")}
              value={persona}
              onChange={(e) => setPersona(e.target.value)}
            />
            <Button type="button" onClick={() => void savePersona()} disabled={saveState === "saving"}>
              {saveLabel}
            </Button>
          </div>
        )}

        {active === "stripe" && (
          <div className="flex flex-col gap-3">
            <p className="text-sm text-secondary">
              {t("stripeHint")}
              {settings.stripeConnected && (
                <span className="ml-2 text-accent">✓ Connected</span>
              )}
            </p>
            <Button onClick={() => void startStripeConnect()} disabled={connecting}>
              {connecting ? t("openingStripe") : t("connectStripe")}
            </Button>
            {connectError && (
              <p role="alert" className="text-sm text-error">
                {connectError}
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
