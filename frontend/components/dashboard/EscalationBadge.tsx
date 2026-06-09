import { TriangleAlert } from "lucide-react";
import { Badge } from "@/components/ui/Badge";

export function EscalationBadge() {
  return (
    <Badge tone="error" className="gap-1">
      <TriangleAlert className="h-3 w-3" />
      Needs you
    </Badge>
  );
}
