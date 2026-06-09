import { Inbox } from "lucide-react";

export function LoadingState({ label = "Loading…" }: { label?: string }) {
  return (
    <div className="flex flex-col gap-3" aria-busy="true" aria-live="polite">
      {[0, 1, 2].map((i) => (
        <div key={i} className="h-16 animate-pulse rounded-lg bg-elevated" />
      ))}
      <span className="sr-only">{label}</span>
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div role="alert" className="rounded-lg bg-surface p-6 text-sm text-error">
      {message}
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="ml-3 rounded-pill border border-border-default px-3 py-1 text-secondary hover:bg-elevated"
        >
          Retry
        </button>
      )}
    </div>
  );
}

export function EmptyState({ title, hint }: { title: string; hint?: string }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-lg bg-surface p-10 text-center">
      <Inbox className="h-8 w-8 text-tertiary" />
      <p className="font-semibold text-primary">{title}</p>
      {hint && <p className="max-w-sm text-sm text-secondary">{hint}</p>}
    </div>
  );
}
