import { ResourceGrid, type ResourceCell } from "@/components/dashboard/ResourceGrid";
import { BookingCard } from "@/components/dashboard/BookingCard";

// Placeholder data — Phase 5 fetches the tenant's resources + today's bookings from the API.
const resources: ResourceCell[] = [
  { id: "1", name: "Chair 1", state: "confirmed" },
  { id: "2", name: "Chair 2", state: "pending" },
  { id: "3", name: "Chair 3", state: "free" },
  { id: "4", name: "Chair 4", state: "free" },
];

export default function TodayPage() {
  return (
    <div className="flex flex-col gap-10">
      <div>
        <h1 className="text-3xl font-bold text-primary">Today</h1>
        <p className="mt-1 text-sm text-secondary">
          Live bookings appear here the moment they happen.
        </p>
      </div>

      <ResourceGrid resourceLabelPlural="Chairs" resources={resources} />

      <section>
        <h2 className="text-lg font-semibold text-primary">Upcoming</h2>
        <div className="mt-4 flex flex-col gap-3">
          <BookingCard time="2:30 PM" customerName="Amy N." detail="Gel Manicure" status="Confirmed" />
          <BookingCard time="3:15 PM" customerName="Linh P." detail="Pedicure" status="PendingDeposit" />
        </div>
      </section>
    </div>
  );
}
