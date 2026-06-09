import { SettingsForm } from "@/components/dashboard/SettingsForm";

export default function SettingsPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-bold text-primary">Settings</h1>
      {/* resourceLabelPlural comes from the tenant's vertical_config in Phase 5. */}
      <SettingsForm resourceLabelPlural="Chairs" />
    </div>
  );
}
