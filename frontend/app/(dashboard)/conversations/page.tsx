import { ConversationList, type ConversationSummary } from "@/components/dashboard/ConversationList";

const conversations: ConversationSummary[] = [
  { id: "1", customerName: "Amy N.", preview: "Can I move to 4pm?", unread: true, escalated: false },
  { id: "2", customerName: "Minh T.", preview: "Table for 6 tonight", unread: false, escalated: true },
];

export default function ConversationsPage() {
  return (
    <div className="flex h-[calc(100vh-4rem)] -m-8">
      <ConversationList conversations={conversations} />
      <div className="flex flex-1 items-center justify-center text-sm text-tertiary">
        Select a conversation
      </div>
    </div>
  );
}
