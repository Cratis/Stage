---
title: Connect a backend to Stage
description: Turn on the Cratis Stage extension protocol, point Stage at an extension backend, and understand what the handshake checks, what is logged and what is not yet available.
---

The extension protocol is off by default. Nothing listens and no backend is called until you configure it, and without it abilities answer that
nothing was sent, exactly as before.

## Turn it on

Configure Stage under `Stage:Extensions`, in `cratis-stage.json`, `appsettings` or the environment (`Stage__Extensions__HostPort`):

```json
{
  "Stage": {
    "Extensions": {
      "HostPort": 50100,
      "HostAddress": "127.0.0.1",
      "AdvertisedHostEndpoint": "",
      "DescribeTimeout": "00:00:10",
      "Backends": {
        "Slack": { "Endpoint": "http://localhost:50051" }
      }
    }
  }
}
```

| Option | Default | Meaning |
| --- | --- | --- |
| `HostPort` | `0` (off) | The port Stage's host services listen on. Setting it turns the protocol on. |
| `HostAddress` | `127.0.0.1` | The address they listen on. Keep loopback unless an extension runs in another container or machine. |
| `AdvertisedHostEndpoint` | empty | The address extensions are told to reach the host services on, when it differs from where they listen, such as `http://stage:50100`. Empty means `http://<HostAddress>:<HostPort>`. |
| `DescribeTimeout` | 10 seconds | How long the handshake with a backend may take. |
| `Backends` | none | The backend of each system by name: its gRPC `Endpoint`. |

Settings and secrets the extension reads come from configuration under `systems:<System>:<name>`, for example the environment variable
`systems__Slack__channel` or `systems__Slack__token`. See [Host services](host-services.md#settings-and-secrets).

## What happens on the first call

1. A model invokes an ability of the system. Stage finds the backend in `Backends`.
2. Stage calls `Describe` with the host endpoint and a credential for the system.
3. The backend answers what it realizes. Stage checks it against the contract and checks the protocol major.
4. Stage keeps the connection and sends the attempt. A mismatch, an unreachable backend or a timed-out handshake is `NotSent`, with the reason in the invocation's record.

## Run order

Start the backend first or let Stage retry: an unreachable backend is a `NotSent` answer and the invocation retries it by its effect class's
rules. Start Stage with the host port configured before the backend is asked to call it, because the backend only learns the endpoint at the handshake.

## Security notes

- The host services are **plaintext HTTP/2 on loopback by default**. If a backend runs elsewhere, keep the traffic on a private network or behind
  a mesh or proxy that provides TLS. Native TLS for the host services is not part of version one. Stage can call a backend at an `https://` endpoint.
- The session token is the extension's only credential for the host services. It is delivered in the handshake and nowhere else. A backend should
  keep it in memory.
- The inbound HTTP endpoint (`/extensions/<System>/...`) is **anonymous** and only exists when the protocol is on. A backend must verify what it receives.
- Cookies and authorization headers of Stage's own users are never forwarded to a backend.

## What is available today

The protocol, the host services and the SDKs are complete. Three inputs come from other work and are empty until it lands, so no model drives them yet:

| Input | Filled by | Until then |
| --- | --- | --- |
| The abilities each system declares (`IAbilityCatalog`) | Building the catalog from the bound model. The Screenplay syntax for `system` and `ability` has shipped ([Cratis/Screenplay#1250](https://github.com/Cratis/Screenplay/issues/1250)); Stage does not read it into the catalog yet. | The catalog is empty, so no model invocation reaches a backend. A backend still connects, serves HTTP and can use the host services. |
| What reacts to each trigger (`IContractTriggerRoutes`) | Reactions and triggers declared on a system, and the inbound traffic that raises them (Cratis/Stage#1068) | `RaiseTrigger` answers `NOT_ROUTED`. |
| Finding and starting a backend from a locked package, lazily | Cratis/Stage#1064 and Cratis/Stage#1066 | Backends are found in the `Backends` configuration above, and run where you run them. |

Each is a seam in the host: register your own `IExtensionEndpoints`, `IAbilityCatalog` or `IContractTriggerRoutes` before the extension services, and
Stage keeps it.

## See also

- [Protocol reference](protocol.md)
- [Durable ability invocations](../reference/ability-invocations.md)
