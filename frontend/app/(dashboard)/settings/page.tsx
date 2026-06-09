"use client";

import { useApiData } from "@/hooks/useApiData";
import { SettingsForm } from "@/components/dashboard/SettingsForm";
import { LoadingState, ErrorState } from "@/components/ui/States";

type Settings = {
  name: string;
  businessType: string;
  timezone: string;
  resourceLabelPlural: string;
};

export default function SettingsPage() {
  const { data, loading, error, reload } = useApiData<Settings>("/api/settings");

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-3xl font-bold text-primary">Settings</h1>
        {data && (
          <p className="mt-1 text-sm text-secondary">
            {data.name} · {data.businessType} · {data.timezone}
          </p>
        )}
      </div>

      {loading && <LoadingState label="Loading settings" />}
      {error && <ErrorState message={error} onRetry={reload} />}
      {!loading && !error && (
        <SettingsForm resourceLabelPlural={data?.resourceLabelPlural ?? "Resources"} />
      )}
    </div>
  );
}
