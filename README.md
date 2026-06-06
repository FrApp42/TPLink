## TPLink C# Lib

Unofficial TP-Link Lib to Read and Send SMS with TP-Link Archer MR600.

Based on projects :
- https://github.com/plewin/tp-link-modem-router
- https://github.com/hercule115/TPLink-Archer


# Read the docs

[Online docs](https://frapp42.github.io/TP-Link/)

## SMS objects

The library exposes typed SMS objects instead of raw strings:

- `SmsToSend` — what you send: one or several `Recipients` and a `Message`.
- `InboxSms` — a received SMS: `Index`, `From`, `Content`, `ReceivedTime`, `Unread`, `Order`.
- `OutboxSms` — a sent SMS: `Index`, `To`, `Content`, `SendTime`, `Order`.
- `SmsSendResult` — the per-recipient result of a send: `Recipient`, `Status`, `IsSuccess`.

`Order` is the 1-based position of the message in the list the router returned. It is the
value to use with `MarkAsReadAsync` / `DeleteInboxAsync` / `DeleteOutboxAsync` (not `Index`).

## Send

```csharp
using FrApp42.TPLink;
using FrApp42.TPLink.Models;

Client client = new(url, username, password);

// Single recipient
await client.SendAsync(new SmsToSend("0606060606", "Hello"));

// Several recipients, same message
SmsToSend sms = new()
{
    Recipients = { "0606060606", "0707070707" },
    Message = $"Hello {DateTime.Now}"
};

List<SmsSendResult> results = await client.SendAsync(sms);
foreach (SmsSendResult result in results)
    Console.WriteLine($"{result.Recipient} -> {result.Status}");
```

The original single-recipient helpers still work:

```csharp
Status status = client.Send("0606060606", "Hello");
```

## Read

```csharp
// Received SMS (inbox)
List<InboxSms> inbox = await client.GetInboxAsync();          // all
List<InboxSms> unread = await client.GetInboxAsync(true);     // unread only
List<InboxSms> unread2 = await client.GetUnreadAsync();       // dedicated endpoint

foreach (InboxSms message in inbox)
    Console.WriteLine($"#{message.Index} from {message.From} on {message.ReceivedTime}: {message.Content}");

// Sent SMS (outbox)
List<OutboxSms> outbox = await client.GetOutboxAsync();
```

## Manage

```csharp
// Use the Order of a message obtained from a read just before
await client.MarkAsReadAsync(inbox[0].Order);
await client.DeleteInboxAsync(inbox[0].Order);
await client.DeleteOutboxAsync(outbox[0].Order);
```

> Note: the router only exposes the last messages it kept (no pagination), and the
> mark-as-read / delete operations are stateful: call them right after a read.
