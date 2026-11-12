# Execution tiers and activation

Stage runs an extension's backend the way Orleans runs a grain: it starts when something needs it, it stops when it has been idle for a while, and
how it runs depends on what it is. Fifty extensions do not mean fifty always-on containers.

This page is for the person who deploys Stage and for the author of a contract. It covers the four execution tiers, the activation policy and who
may override it, what activates a backend, and what the Workbench shows. It applies to backends that come from [extension packages](packages.md);
a backend you start yourself and list under `Stage:Extensions:Backends` keeps running where you put it, as described in
[Connect a backend to Stage](connect-a-backend.md).

## Execution tiers

The tier says how a backend's process is laid out. The contract says what the backend is, and the deployment decides what may share a host.

| Tier | Process layout | When Stage picks it |
| --- | --- | --- |
| `None` | No process. | The package has no `backend` line: it is frontend-only or declarative. |
| `SharedHost` | One host per runtime (`dotnet`, `node`, `jvm`) loads many extensions and isolates each inside it. | The runtime is `dotnet`, `node` or `jvm`, and the deployment allows the package to share a host. |
| `Native` | A small process of its own for the one extension. | The runtime is `native` or `go`. |
| `Container` | Its own image and process. | Everything else: a shared-capable runtime the deployment does not allow to share, or any other runtime. |

Sharing follows trust. A package may share a host only when its publisher or its resolved reference matches `Execution:Sharing`. Anything else gets a
container of its own, and a system that needs a container the deployment does not configure is refused with a message that names the setting, so it never
quietly runs somewhere less isolated.

```json
{
  "Stage": {
    "Extensions": {
      "HostPort": 50100,
      "Execution": {
        "Sharing": {
          "Publishers": [ "https://github.com/cratis" ],
          "References": [ "ghcr.io/acme/*" ]
        },
        "Hosts": {
          "dotnet": { "Command": "/opt/stage/hosts/dotnet-host", "Arguments": [ "--quiet" ] }
        },
        "Containers": {
          "Ledger": { "Image": "ghcr.io/acme/extension-ledger:3", "Arguments": [ "--memory", "256m" ] }
        }
      }
    }
  }
}
```

A pattern is an exact value or a prefix that ends in `*`, the same rule the [trust policy](packages.md) uses.

### How each tier starts a backend

Stage unpacks the backend from bytes it verifies against the locked digest again, on every start, and removes the folder when the backend stops. It then tells the
backend where to listen with environment variables and waits until the address accepts connections, so the call that triggered the start does not race it.

| Variable | Meaning |
| --- | --- |
| `STAGE_EXTENSION_LISTEN` | The address to listen on, as `host:port`. A container is told `0.0.0.0:8080`. |
| `STAGE_EXTENSION_SYSTEM` | The name of the system the backend serves. |
| `STAGE_EXTENSION_PACKAGE_DIGEST` | The digest of the package the backend came from. |

The SDK runners take the address to listen on in code, so a backend reads `STAGE_EXTENSION_LISTEN` itself and passes it on.

- **Native.** The package's entry point is executed in its unpacked folder. Stopping it kills the process tree.
- **Container.** Stage runs `docker run --rm` with the image, the published port and the variables above, plus the `Arguments` you configure for the system. Stopping it runs `docker stop`.
  `Command` names a client with the same command lines when you do not use `docker`. The container reaches Stage's host services on `AdvertisedHostEndpoint`, so set it to an address the
  container can reach.
- **Shared host.** Stage starts the configured host once for a runtime, when the first extension of that runtime needs it, and stops it with the last. The host is one process that
  loads many extensions and isolates each one, so an extension that fails is reported by name and the others keep running. Stage and the host talk in lines on the host's input and output:

  | Stage writes | The host answers |
  | --- | --- |
  | `load <system> <port> <folder>` | `loaded <system>`, or `failed <system> <reason>` |
  | `unload <system>` | `unloaded <system>` |
  | | `exited <system> <reason>` at any time, when an extension it hosts fails |

  Any other line is the host's log. Closing the host's input asks it to exit.

## Activation policy

The policy says when a backend runs: always, or on demand until it has been idle for a while. Three places can say it, and the most specific wins.

| Order | Where | Example |
| --- | --- | --- |
| 1 | The consumer's `activation` line on the system in the model. | `activation always` |
| 2 | The default in the extension's contract. | `activation on demand idle after 5 minutes` |
| 3 | The deployment: `Execution:IdleTimeout`, ten minutes by default. | |

An override that states no idle timeout keeps the contract's. An extension with no `activation` line is on demand. The two lines are described in
[Screenplay systems and settings](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/environments.md) and
[extension contracts](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/extension-contracts.md).

`always` keeps the backend running: Stage starts it when the host starts and starts it again when it fails. `on demand` stops it when no call has used it for the idle timeout and nothing is in flight.

## Lazy activation

A backend has four states.

| State | Meaning |
| --- | --- |
| `Inactive` | Nothing runs. The next need starts it. |
| `Starting` | The backend is being started and does not accept calls yet. |
| `Active` | The backend runs and accepts calls. |
| `Stopping` | The backend is being stopped. |

- **First need.** The first ability call, proxied request or inbound delivery for a system starts its backend and waits until it accepts connections, then connects and does the handshake.
  A call that arrives while the backend is starting waits for that start; it never starts a second one.
- **Idle stop.** Every `Execution:SweepInterval` (fifteen seconds) Stage stops the on-demand backends that have been idle for their timeout and have no call in flight. A call that takes longer
  than the timeout keeps its backend running until it ends.
- **No lost invocation.** A call that finds its backend stopped starts it again and then goes through. Durable invocations wait in the outbox, so a cold start is invisible to them; see
  [ability invocations](../reference/ability-invocations.md) and the Choreography
  [extension invocations](https://github.com/Cratis/Choreography/blob/main/Documentation/workflows/extension-invocations.md). Until the backend accepts the call, nothing was sent, so a start that
  fails is reported as not sent and is safe to retry.
- **Inbound webhooks.** A webhook is stored and acknowledged without waiting for a cold start. The backend starts when the stored delivery is dispatched.
- **A backend that fails.** Stage notices that the process is gone, lets go of the connection and shows the backend as inactive. The next call starts it again, and an `always` backend is started
  again by the next sweep.
- **A backend that cannot start.** The reason is kept and shown in the Workbench, and the next call tries again.

## Frontend-aware activation

Stage knows which screens place which extension components, so it can start a backend before the browser makes its first call. Which of two moments triggers that is decided here.

**Decision: reaching the screen activates the backend. Mounting a component does not.**

- **The route is known earlier.** Stage learns about the screen when the browser navigates to it, before anything renders. The backend starts while the screen loads, so the first call from a
  component finds it running. A mount trigger can only start the backend once the component already exists and is about to call.
- **Mounting is late, conditional and repeated.** A component in a tab, a collapsed panel or a lazy region mounts when the user opens it, and again each time it is remounted. Starting on mount would
  make the cold start the first thing the user waits for.
- **It is the conservative choice.** A backend that starts for a screen whose component never renders costs one start that the idle timeout then reclaims. A backend that is not started in time costs a visible
  delay on the first call. The first call still activates the backend when nothing else did, so navigation is an optimization that cannot make a call fail.
- **It grants nothing.** Reaching a screen starts only the backends of systems that screen places, for systems with a backend. It does not call anything, and every call is still authorized as before.

The browser tells Stage with one request, which answers at once and does not wait for the start:

```text
POST /stage/extensions/activation/screens/invoices/overview
```

```json
{ "backends": [ { "system": "Mail", "state": "Starting" } ] }
```

Which systems a screen places is the model's to say. It comes from `IScreenExtensionUsage`, which Stage fills from the screens' extension components once the language lets a screen use them
([Cratis/Screenplay#1255](https://github.com/Cratis/Screenplay/issues/1255)). Until then no screen places a component, so reaching a screen activates nothing, and a backend starts on its first call.

## What the Workbench shows

`GET /stage/extensions/activation` lists every extension system with its package, tier, state, policy, last activation, last stop, calls in flight and, when the backend could not start, the reason.
`/stage/extensions/activation/view` renders the same list as a page that runs no script. Like the [package listing](packages.md), both are tooling endpoints of the sandbox.

## Configuration

`Stage:Extensions:Execution` binds to `ExecutionOptions`.

| Option | Default | Meaning |
| --- | --- | --- |
| `IdleTimeout` | 10 minutes | How long an on-demand backend may be idle when neither the contract nor the model says. |
| `SweepInterval` | 15 seconds | How often idle backends are stopped and always-on backends are checked. |
| `StartTimeout` | 30 seconds | How long a backend may take to accept connections. |
| `StopTimeout` | 10 seconds | How long a stop may take before the process is killed. |
| `ListenAddress` | `127.0.0.1` | The address backends listen on. Change it only when a backend runs in a container or on another machine. |
| `BackendsDirectory` | `backends` in the package cache | Where a backend is unpacked to run. |
| `Sharing:Publishers`, `Sharing:References` | none | Which packages may share a runtime host. |
| `Hosts:<runtime>:Command`, `Arguments` | none | The shared host of a runtime. |
| `Containers:<System>:Image`, `Command`, `Arguments` | none, `docker` | The container of a system that does not share a host. |

## Related

- [Extension backends](index.md) and the [protocol reference](protocol.md).
- [Extension packages](packages.md): what is loaded before anything is activated.
- [Systems, abilities and extensions](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/systems.md), the Screenplay overview of extensions.
- [Settings, secrets and activation](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/environments.md) for the consumer's `activation` line.
- Decision [0159](https://github.com/Cratis/Screenplay/blob/main/decisions/0159-systems-with-abilities-extensions-and-durable-invocations.md), section 8, which sets the tiers.
