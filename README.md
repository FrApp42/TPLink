# TPLink — Lib C# & passerelle API pour Archer MR600

Bibliothèque C# non officielle pour **lire et envoyer des SMS** via un routeur TP-Link Archer MR600,
accompagnée d'une **passerelle REST** prête à l'emploi.

Basé sur :
- https://github.com/plewin/tp-link-modem-router
- https://github.com/hercule115/TPLink-Archer

[Documentation en ligne](https://frapp42.github.io/TP-Link/) · [NuGet `FrApp42.TP-Link`](https://www.nuget.org/packages/FrApp42.TP-Link)

## ✨ Fonctionnalités

| Module | Description |
|--------|-------------|
| **Objets SMS typés** | `SmsToSend`, `InboxSms`, `OutboxSms`, `SmsSendResult` au lieu de chaînes brutes |
| **Envoi** | Un ou plusieurs destinataires pour un même message, avec résultat par destinataire |
| **Lecture** | Boîte de réception (tous / non lus) et boîte d'envoi, propriétés complètes |
| **Gestion** | Marquer comme lu, supprimer un SMS reçu ou envoyé |
| **Passerelle REST** | API HTTP reproduisant le bridge `plewin/tp-link-modem-router` |
| **Swagger** | Interface OpenAPI activable / désactivable via `appsettings.json` |
| **Authentification** | HTTP Basic optionnelle sur la passerelle |

## 🏗️ Architecture

```
TPLink/
├── TP-Link/                # Bibliothèque FrApp42.TP-Link
│   ├── Models/             # SmsToSend, InboxSms, OutboxSms, SmsSendResult, Payload…
│   ├── Enums/              # TP_ACT, TP_CONTROLLERS, Status
│   ├── Client.cs           # API publique (envoi, lecture, gestion)
│   ├── Protocol.cs         # Encodage des trames + parsing des réponses
│   ├── Encryption.cs       # Chiffrement AES/RSA de la session
│   └── ApiRequest.cs       # Couche HTTP
├── TPLink.Gateway/         # Passerelle REST (ASP.NET Core, .NET 8)
│   ├── Controllers/        # SmsController, MonitoringController
│   ├── Configuration/      # RouterOptions, SwaggerOptions
│   ├── Middleware/         # BasicAuthMiddleware
│   ├── Filters/            # ApiExceptionFilter
│   └── Program.cs
├── SMS_Sender/             # Exemple console
├── TP-Link.Test/           # Tests
└── TP-Link.Docs/           # Documentation (frapp42.github.io)
```

## 🛠️ Technologies

- **.NET 8** — Framework cible
- **ASP.NET Core** — Passerelle REST (`TPLink.Gateway`)
- **Swashbuckle.AspNetCore** — Documentation OpenAPI / Swagger UI
- **System.Text.RegularExpressions** — Parsing des trames du routeur

## ⚙️ Prérequis

- .NET 8 SDK
- Un routeur TP-Link Archer MR600 accessible sur le réseau
- Les identifiants d'administration du routeur

## 🚀 Installation

### Bibliothèque (NuGet)
```bash
dotnet add package FrApp42.TP-Link
```

### Depuis les sources
```bash
git clone https://github.com/FrApp42/TPLink.git
cd TPLink
dotnet build
```

## 📨 Utilisation

### Objets SMS

La bibliothèque expose des objets typés plutôt que des chaînes brutes :

- `SmsToSend` — ce que l'on envoie : un ou plusieurs `Recipients` et un `Message`.
- `InboxSms` — un SMS reçu : `Index`, `From`, `Content`, `ReceivedTime`, `Unread`, `Order`.
- `OutboxSms` — un SMS envoyé : `Index`, `To`, `Content`, `SendTime`, `Order`.
- `SmsSendResult` — le résultat par destinataire d'un envoi : `Recipient`, `Status`, `IsSuccess`.

`Order` est la position (à partir de 1) du message dans la liste renvoyée par le routeur.
C'est cette valeur qu'utilisent `MarkAsReadAsync` / `DeleteInboxAsync` / `DeleteOutboxAsync`
(et non `Index`).

### Envoi

```csharp
using FrApp42.TPLink;
using FrApp42.TPLink.Models;

Client client = new(url, username, password);

// Un seul destinataire
await client.SendAsync(new SmsToSend("0606060606", "Hello"));

// Plusieurs destinataires, même message
SmsToSend sms = new()
{
    Recipients = { "0606060606", "0707070707" },
    Message = $"Hello {DateTime.Now}"
};

List<SmsSendResult> results = await client.SendAsync(sms);
foreach (SmsSendResult result in results)
    Console.WriteLine($"{result.Recipient} -> {result.Status}");
```

Les helpers d'origine à destinataire unique fonctionnent toujours :

```csharp
Status status = client.Send("0606060606", "Hello");
```

### Lecture

```csharp
// SMS reçus (inbox)
List<InboxSms> inbox = await client.GetInboxAsync();          // tous
List<InboxSms> unread = await client.GetInboxAsync(true);     // non lus uniquement
List<InboxSms> unread2 = await client.GetUnreadAsync();       // endpoint dédié

foreach (InboxSms message in inbox)
    Console.WriteLine($"#{message.Index} de {message.From} le {message.ReceivedTime} : {message.Content}");

// SMS envoyés (outbox)
List<OutboxSms> outbox = await client.GetOutboxAsync();
```

### Gestion

```csharp
// Utilise l'Order d'un message obtenu lors d'une lecture juste avant
await client.MarkAsReadAsync(inbox[0].Order);
await client.DeleteInboxAsync(inbox[0].Order);
await client.DeleteOutboxAsync(outbox[0].Order);
```

> Note : le routeur n'expose que les derniers messages qu'il a conservés (pas de pagination),
> et les opérations marquer comme lu / supprimer sont *stateful* : appelez-les juste après une lecture.

## 🌐 Passerelle API (`TPLink.Gateway`)

Passerelle REST ASP.NET Core reproduisant l'API du bridge `plewin/tp-link-modem-router`.

| Méthode  | Route                          | Description                                       |
|----------|--------------------------------|---------------------------------------------------|
| `GET`    | `/sms/inbox?unread=true|false` | SMS reçus (derniers conservés), filtre optionnel  |
| `PATCH`  | `/sms/inbox/{smsOrderNumber}`  | Marquer le n-ième SMS reçu comme lu (base 1)      |
| `DELETE` | `/sms/inbox/{smsOrderNumber}`  | Supprimer le n-ième SMS reçu (base 1)             |
| `GET`    | `/sms/outbox`                  | SMS envoyés (derniers conservés)                  |
| `POST`   | `/sms/outbox`                  | Envoyer un SMS (`{ "to", "content" }`, JSON/form) |
| `DELETE` | `/sms/outbox/{smsOrderNumber}` | Supprimer le n-ième SMS envoyé (base 1)           |
| `GET`    | `/monitoring/metrics`          | Métriques Prometheus (WIP)                        |

Toutes les routes sont montées sous `/api/v1`. Les réponses suivent les enveloppes du bridge
de référence : succès `{ "status": 200, "data": ... }`, erreur `{ "status": 500, "exception": { "name", "message" } }`.

### Lancer

```bash
dotnet run --project TPLink.Gateway
```

### Configuration (`appsettings.json`)

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

- **`Swagger:Enabled`** — active ou désactive le document Swagger et l'interface Swagger UI
  (servie sur `/{RoutePrefix}`, par défaut `/swagger`).
- **`Router`** — paramètres de connexion au routeur.
- **`Authentication:Users`** — identifiants HTTP Basic protégeant `/api/*` (vide = désactivé ;
  Swagger n'est jamais protégé).

## 👥 Auteurs

- [AnthoDingo](https://github.com/AnthoDingo)
- [Sikelio](https://github.com/Sikelio)

## 📄 Licence

Ce projet est sous licence **GPL-3.0** — voir [LICENSE](./LICENSE.MD).
