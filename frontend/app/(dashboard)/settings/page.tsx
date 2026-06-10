"use client";

import { useTranslations } from "next-intl";
import { useApiData } from "@/hooks/useApiData";
import {
  SettingsForm,
  type OfferingRow,
  type ResourceRow,
  type SettingsBundle,
} from "@/components/dashboard/SettingsForm";
import { LoadingState, ErrorState } from "@/components/ui/States";

export default function SettingsPage() {
  const t = useTranslations("dashboard");
  const settings = useApiData<SettingsBundle>("/api/settings");
  const offerings = useApiData<OfferingRow[]>("/api/offerings");
  const resources = useApiData<ResourceRow[]>("/api/resources");

  const loading = settings.loading || offerings.loading || resources.loading;
  const error = settings.error ?? offerings.error ?? resources.error;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-3xl font-bold text-primary">{t("settingsTitle")}</h1>
        {settings.data && (
          <p className="mt-1 text-sm text-secondary">
            {settings.data.name} · {settings.data.businessType} · {settings.data.timezone}
          </p>
        )}
      </div>

      {loading && <LoadingState label={t("settingsTitle")} />}
      {error && (
        <ErrorState
          message={error}
          onRetry={() => {
            settings.reload();
            offerings.reload();
            resources.reload();
          }}
        />
      )}
      {!loading && !error && settings.data && Array.isArray(offerings.data) && Array.isArray(resources.data) && (
        <SettingsForm
          settings={settings.data}
          offerings={offerings.data}
          resources={resources.data.map((r) => ({
            id: r.id,
            name: r.name,
            capacity: r.capacity,
            isActive: r.isActive,
          }))}
          onSettingsSaved={settings.reload}
          onOfferingsChanged={offerings.reload}
          onResourcesChanged={resources.reload}
        />
      )}
    </div>
  );
}
