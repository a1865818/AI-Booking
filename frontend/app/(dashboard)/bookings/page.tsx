import { BookingCard } from "@/components/dashboard/BookingCard";

export default function BookingsPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-bold text-primary">Bookings</h1>
      <div className="flex flex-col gap-3">
        <BookingCard time="Today 2:30 PM" customerName="Amy N." detail="Gel Manicure" status="Confirmed" />
        <BookingCard time="Today 3:15 PM" customerName="Linh P." detail="Pedicure" status="PendingDeposit" />
        <BookingCard time="Tomorrow 7:00 PM" customerName="Minh T." detail="Table · party of 6" status="Confirmed" />
      </div>
    </div>
  );
}
