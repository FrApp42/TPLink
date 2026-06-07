# TPLink.Gateway

REST API gateway for the TP-Link Archer MR600, reproducing the API of
[`plewin/tp-link-modem-router`](https://github.com/plewin/tp-link-modem-router)
on top of the `FrApp42.TP-Link` library.

## Endpoints (mounted under `/api/v1`)

| Method   | Route                          | Description                                        |
| -------- | ------------------------------ | -------------------------------------------------- |
| `GET`    | `/sms/inbox?unread=true|false` | Received SMS (last kept), optional unread filter   |
| `PATCH`  | `/sms/inbox/{smsOrderNumber}`  | Mark the n-th received SMS as read (1-based)       |
| `DELETE` | `/sms/inbox/{smsOrderNumber}`  | Delete the n-th received SMS (1-based)             |
| `GET`    | `/sms/outbox`                  | Sent SMS (last kept)                               |
| `POST`   | `/sms/outbox`                  | Send a new SMS (`{ "to", "content" }`, JSON/form)  |
| `DELETE` | `/sms/outbox/{smsOrderNumber}` | Delete the n-th sent SMS (1-based)                 |
| `GET`    | `/monitoring/metrics`          | Prometheus metrics (WIP)                           |

Success responses use `{ "status": 200, "data": ... }`; errors use
`{ "status": 500, "exception": { "name", "message" } }`, like the reference bridge.

The `inbox`/`outbox` order-based routes are stateful at the router's end: call them
right after the matching `GET`, using the 1-based `order` from that read (not the SMS index).

## Configuration (`appsettings.json`)

```json
{
  "Swagger": {
    "Enabled": true,
    "Title": "Archer MR600 bridge API",
    "Version": "v1",
    "Description": "Open Source API bridge for Archer MR600",
    "RoutePrefix": "swagger"
  },
  "Router": {
    "Url": "http://192.168.1.1",
    "Login": "admin",
    "Password": ""
  },
  "Authentication": {
    "Users": { "admin": "changeme" }
  }
}
```

- **`Swagger:Enabled`** — turns the Swagger JSON document and Swagger UI on or off.
  When `true`, the UI is served at `/{Swagger:RoutePrefix}` (default `/swagger`).
- **`Router`** — router connection settings.
- **`Authentication:Users`** — HTTP Basic credentials protecting `/api/*`
  (reproduces the reference `api_users`). Leave empty to disable authentication;
  Swagger is never protected.

## Run

```bash
dotnet run --project TPLink.Gateway
```
