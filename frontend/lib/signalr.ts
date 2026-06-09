import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";

const SIGNALR_URL =
  process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5126/hubs/booking";

/**
 * Builds a connection to the BookingHub. The JWT is supplied via `accessTokenFactory`; the
 * backend reads it from the `access_token` query string on the WebSocket handshake.
 */
export function createBookingConnection(getAccessToken: () => string | null): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      accessTokenFactory: () => getAccessToken() ?? "",
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}

export type BookingHubEvent =
  | "BookingCreated"
  | "BookingStatusChanged"
  | "NewConversationMessage"
  | "EscalationRequired"
  | "AiToggled";
