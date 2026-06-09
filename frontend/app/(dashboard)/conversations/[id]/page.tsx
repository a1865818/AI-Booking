import { ConversationList, type ConversationSummary } from "@/components/dashboard/ConversationList";
import { ConversationDetail } from "@/components/dashboard/ConversationDetail";
import type { Message } from "@/hooks/useConversation";

const conversations: ConversationSummary[] = [
  { id: "1", customerName: "Amy N.", preview: "Can I move to 4pm?", unread: true, escalated: false },
  { id: "2", customerName: "Minh T.", preview: "Table for 6 tonight", unread: false, escalated: true },
];

const messages: Message[] = [
  { id: "m1", conversationId: "1", direction: "Inbound", body: "Hi, can I book a gel manicure tomorrow?", createdAt: "" },
  { id: "m2", conversationId: "1", direction: "Outbound", body: "Of course! We have 2:30 PM or 4:00 PM open. Which works?", createdAt: "" },
];

export default async function ConversationDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const active = conversations.find((c) => c.id === id) ?? conversations[0];

  return (
    <div className="flex h-[calc(100vh-4rem)] -m-8">
      <ConversationList conversations={conversations} activeId={id} />
      <ConversationDetail customerName={active.customerName} messages={messages} />
    </div>
  );
}
