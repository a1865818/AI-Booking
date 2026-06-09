import { Badge } from "@/components/ui/Badge";

type Status = "PendingDeposit" | "Confirmed" | "Completed" | "Cancelled" | "NoShow";

const tone: Record<Status, "accent" | "warning" | "neutral" | "error"> = {
  PendingDeposit: "warning",
  Confirmed: "accent",
  Completed: "neutral",
  Cancelled: "error",
  NoShow: "error",
};

export function BookingCard({
  time,
  customerName,
  detail,
  status,
}: {
  time: string;
  customerName: string;
  detail: string;
  status: Status;
}) {
  return (
    <div className="flex items-center justify-between rounded-lg bg-surface p-4">
      <div>
        <p className="font-semibold text-primary">{customerName}</p>
        <p className="text-sm text-secondary">
          {time} · {detail}
        </p>
      </div>
      <Badge tone={tone[status]}>{status}</Badge>
    </div>
  );
}
