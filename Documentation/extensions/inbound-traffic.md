# Inbound traffic: webhooks into contract triggers

Outside systems send traffic to an extension: a payment provider's webhook, a button click, a reply to a message. The sender expects an answer within a
few seconds and often retries when it does not get one. Stage therefore owns the endpoint, keeps what arrived, answers the sender at once and then
delivers the request to the extension. The extension verifies the request and raises the
[triggers its contract declares](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/triggers.md#how-a-name-resolves), such as
`Payments.PaymentSucceeded`. From there the trigger behaves like any other: it starts reactions, captures and workflows, resumes workflows that wait for
it, and reaches screens (`on <System>.<Trigger>`) and components (`context.on`).

This page is the reference for what Stage does with the traffic. The protocol messages are in the [protocol reference](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/protocol.md#inbound-http-handlehttp),
the services an extension calls to raise a trigger are in the [host services reference](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/host-services.md#raise-a-trigger),
and the model side is the [systems and abilities](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md) page of Screenplay.

## The path of a request

```text
sender ──POST /extensions/Payments/webhook──▶ Stage ──▶ stored ──▶ 202 Accepted ──▶ sender
                                               │
                                               └─ dispatcher ──▶ activate the extension (if it is not running)
                                                              ──▶ HandleHttp (raw body and headers, every attempt)
                                                                    │ verify the signature with the secret setting
                                                                    │ RaiseTrigger("PaymentSucceeded", key, correlation)
                                                                    ▼
                                                reactions · captures · workflow starts and waits · screens · components
```

1. **Receive.** A request to `/extensions/<System>/<path>` with a method that may change something (`POST`, `PUT`, `PATCH`, `DELETE`) is stored with its
   method, path, query, raw body and headers, then acknowledged with `202 Accepted`. The sender never waits for the extension, so a cold start, a slow
   handler or a restart does not cost the sender a timeout.
2. **Deliver.** A dispatcher inside Stage delivers stored requests. An extension that is not running is activated first. Every attempt carries the raw
   body exactly as it arrived, so a signature computed over the body verifies.
3. **Verify and raise.** The extension verifies the request with its secret settings and raises contract triggers through the
   [host services](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/host-services.md#raise-a-trigger), or answers a pending `answers later`
   invocation. It answers `200` or `202` when it has taken the request.

A request that only reads (`GET`, `HEAD`, `OPTIONS`) is not stored. It is passed to the extension and answered with what the extension answers, because such
a request asks a question, such as a verification challenge, and the sender needs the answer.

Stage answers the sender itself in these cases, and stores nothing:

| Answer | When |
| --- | --- |
| `404` | The system has no backend, or the extension is running and declares no route for the path. |
| `413` | The body is larger than 1 MiB. |
| `503` with `Retry-After: 60` | More than `MaxOpen` requests wait for delivery, so an anonymous sender cannot fill storage. |

A path that the extension does not declare is only discovered when the extension is running. Until then Stage stores the request and the extension is asked, so
the request ends as **rejected** in the Workbench instead of being answered with `404`.

## What the extension answers

The extension's answer to `HandleHttp` decides what becomes of the stored request:

| The extension answers | The request becomes | Retried |
| --- | --- | --- |
| `2xx` | **Delivered** | No. |
| `4xx` other than `408`, `425` and `429` | **Rejected**: it did not verify, or is not one the extension handles. | **No.** The same request would be refused again. |
| `408`, `425`, `429`, `5xx`, no answer, or the extension cannot be reached | Retrying | Yes, with a growing wait. |
| Every attempt failed | **Failed** | Only when an operator retries it. |

An extension that finds the signature invalid **answers `401`**. That makes Stage reject the request for good and keep it visible, rather than retry it.

Delivery is **at least once**. If Stage stops between the extension taking a request and Stage recording it, the request is delivered again after the restart.
Every attempt carries the same delivery identity, as `invocation_id` and `idempotency_key` in the request context and as `DeliveryId` on the SDK's call, so the
extension deduplicates on it. A trigger raised with a deduplication key is deduplicated by Stage too: raising it again finds the runs the first raise started.

## Security

The endpoint is anonymous by design: the sender of a webhook is not a Stage user. Four things make that safe.

**The extension verifies the request.** Stage cannot know how a provider signs. The extension reads the signing secret from its [secret settings](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/host-services.md#settings-and-secrets),
verifies the signature over the raw body, and answers `401` when it does not verify. Nothing in the model holds a credential. The C# SDK's
[`WebhookSignature`](https://github.com/Cratis/Stage/blob/main/Documentation/extensions/csharp.md#verify-a-webhook-signature) does this with an HMAC-SHA256 and a
comparison that takes the same time however many characters match. An extension raises a trigger only after the request verified.

**Replays are absorbed.** A request that is identical to one Stage already holds, in method, path, query and body, is acknowledged and not delivered again:
while the first is still waiting, and for `ReplayWindow` (24 hours) after it arrived. This also absorbs the sender's own retries. The signature headers are not
part of the comparison, because a sender's retry carries a fresh timestamp and signature. A request that was given up on as **failed** is not a replay: a sender
that retries after every attempt failed is asking for the delivery to be made. A captured request replayed after the window is delivered, so the extension must
also refuse a stale one: sign and check a timestamp with `WebhookSignature.Verify(..., timestamp, ..., now, tolerance)`.

**The trusted actor is the extension, not the sender.** The reactions a trigger starts run as the actor `extension:<System>`, recorded on the run, and the
trigger's causation names the delivery. Only an extension that presented the credential from its handshake can raise a trigger, and only one its own contract
declares. The anonymous sender never becomes the identity a reaction runs as.

**Secrets stay out of storage and out of the Workbench.** `Cookie`, `Authorization`, `Host` and connection headers are never stored or forwarded. The Workbench
lists a request's status, size and header names, never its body or header values.

## Resume a workflow that waits

A trigger starts the reactions that react to it. To also resume workflow runs that wait for it, the extension names what the trigger is about:

```csharp
await call.Host.Trigger("PaymentSucceeded", payloadJson, deduplicationKey: evt.Id, correlationKey: evt.PaymentId);
```

Runs that wait for `Payments.PaymentSucceeded` about the same correlation resume, with the payload as the data of the wait. Without a correlation key only the
reactions run. A trigger that starts nothing and resumes nothing is reported as `NOT_ROUTED`, never silently accepted. A trigger raised before the wait it
is meant for is open resumes nothing and is not remembered: the workflow reads the state it needs when it reaches the wait.

## Watch and retry in the Workbench

The Workbench shows every stored request (the newest 5000), with its status, attempts, when the next attempt is due, and what the last attempt ended with.
The tooling endpoints behind it exist in the sandbox and are not mapped by a published runtime:

| Request | Meaning |
| --- | --- |
| `GET /stage/extensions/deliveries?status=failed` | The requests, newest first. `status` is `pending`, `retrying`, `delivered`, `rejected` or `failed`. |
| `POST /stage/extensions/deliveries/retry` with `{ "id": "..." }` | Puts a failed or rejected request back for another round of attempts. |

## Settings

Under `Stage:Extensions:Inbound`:

| Setting | Default | Meaning |
| --- | --- | --- |
| `MaxAttempts` | `8` | Attempts before a request is shown as failed. |
| `RetryDelay` | `00:00:05` | The wait before the second attempt. Each following wait doubles. |
| `MaxRetryDelay` | `00:15:00` | The longest wait between two attempts. |
| `ReplayWindow` | `24:00:00` | How long an identical request counts as a replay. |
| `MaxOpen` | `1000` | Requests that may wait at once. Past it Stage answers `503`. |
| `PollInterval` | `00:00:02` | How long the dispatcher sleeps when nothing is due. |
| `AttemptTimeout` | `00:00:30` | How long one attempt may take. |

## Limits

- A route is declared by the backend, and Stage learns it at the handshake. A path the extension does not declare is rejected at delivery, not answered with `404` by Stage, until the extension has run once.
- A `POST` is always acknowledged with `202` and an empty body. A provider that requires a synchronous answer to a `POST`, such as a challenge handshake, is not served yet.
- Only the C# SDK has `WebhookSignature` and the correlation key; the other SDKs read the secret and compare the signature themselves.
