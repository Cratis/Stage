---
title: Run a Cratis Stage sandbox with Docker
description: The Cratis Stage Docker container runs a Cratis Screenplay model as a private sandbox with HTTP commands, queries, screens, and in-memory event storage.
---

Cratis Stage runs a Cratis Screenplay `.play` model as an application sandbox. Write the model, start a session
with one command, and use the HTTP commands and queries it describes before implementing the application.
You can use authored or default Scene screens, inspect read state, and follow recorded events in Cratis Chronicle.
The `cratis/stage` container starts Cratis Arc and an in-memory Chronicle kernel beside the application, so you
do not need a separate database or Chronicle server. Replacing the container loses session state. This executes
the model; it does not render code or test a generated application.

:::caution[Supported subset, not a production host]
The default semantic engine executes supported model behavior directly. It does not execute every Screenplay
construct, even when the model compiles. Admission checks identify what Stage can run; read the
[semantic execution limits](#opt-in-to-semantic-execution) before relying on an operation. Use this container
for temporary model exploration, not durable application hosting.
:::

## Start a disposable session

You need Docker and a complete `.play` application, either one file or a folder of files. If you do not have a model,
[create a Screenplay model](/screenplay/getting-started/) first. Include its referenced
concepts and policies, commands with event productions, and read models with projections and queries. A read model
is a view built from events; a projection describes how each event updates that view. A getting-started excerpt
alone may lack the declarations needed to run. Screens are optional: Stage can compose default screens from admitted
commands and read models. An empty folder exits before either process starts.

### Prepare a small model

For a first session, save this standalone example as `screenplays/application.play`. It keeps the `Home` feature
and its `CustomerId` concept from the CLI's shell model: one command produces a fact, a projection builds a row,
and a screen reads the query. After sending `GreetCustomer`, you will look for its text in `AllWelcomes`.
The example declares no authentication or custom UI packages.

```screenplay title="screenplays/application.play"
concept CustomerId : Uuid

module Home
  feature Welcome
    slice StateView WelcomeList
      readmodel WelcomeRow
        welcomeId CustomerId
        text String
      query AllWelcomes => observable WelcomeRow[]
      projection WelcomeList => WelcomeRow
        from CustomerGreeted
          welcomeId = $eventSourceId
          text = text
      screen Welcome
        title "Welcome"
        data WelcomeRow[] via query AllWelcomes
        table WelcomeRow
          column text label "Text"
    slice StateChange GreetCustomer
      command GreetCustomer
        id CustomerId identifier
        text String
        produces CustomerGreeted
          for id
          text = text
      event CustomerGreeted
        text String
```

`id` selects the command's event source. The projection takes that identity from `$eventSourceId`, so the row's
`welcomeId` connects the command to the read result; `text` comes from `CustomerGreeted`. The authored Welcome
screen uses `AllWelcomes`. Once the session starts, send `GreetCustomer` through the interactive API reference,
[Scalar](http://localhost:9090/scalar/v1). This model has an authored screen, so Stage does not compose default
command screens; use Scalar for the command.

### Start with the CLI

With the [Cratis CLI installed](/cli/getting-started/) and your complete model in `./screenplays`, run:

```bash
cratis run ./screenplays
```

The CLI mounts only the selected folder, read-only, chooses its compatible Stage image, and reports readiness or
model refusals. First use may pull the image. For a model without declared authentication, it publishes the application
on loopback port `9090` and Workbench on loopback port `35000`. Keep these defaults: `--allow-remote` exposes the
unauthenticated standalone sandbox without adding authentication. If your model declares authentication, follow the
[authenticated CLI flow](/cli/reference/run/) instead; its entry address and configuration differ.

When the CLI reports readiness, continue with [Use the application](#use-the-application) to submit
`GreetCustomer` and find the resulting row. If you prefer to manage the container yourself, use the Docker command
below instead of `cratis run`.

### Start directly with Docker

:::caution[Keep the session private]
Docker enables [sandbox identity](#sandbox-identity): modeled API requests use the selected persona without login.
The command below binds both published ports to `127.0.0.1`. Keep model operations and session data off public networks.
:::

1. Open a terminal in the folder containing your model. The mount below is read-only and searched recursively.
2. Start the container:

   ```bash title="Terminal — start a model session"
   docker run --rm -p 127.0.0.1:9090:9090 -p 127.0.0.1:35000:35000 -v "$PWD":/eventmodel:ro cratis/stage:latest
   ```

3. After startup ([wait for the session to settle](#wait-for-the-session-to-settle) if you are scripting it), open
   [Scalar](http://localhost:9090/scalar/v1) and check the model's mapped commands and queries.
   The [OpenAPI document](http://localhost:9090/openapi/v1.json) lists the session's model API. Check the
   [session status](http://localhost:9090/stage/status) for admission issues before
   treating a responding HTTP endpoint as a usable session. These checks show what the host exposes, not whether
   every modeled behavior is implemented.

The direct Docker command uses the moving `latest` tag. The CLI selects the Stage version it renders with;
do not assume a published image has every capability described by current source.

### Use an external Chronicle kernel

By default the container runs its own Chronicle kernel with in-memory storage, so every session starts empty and ends with the container. To keep the event log, run a Chronicle that stores in a database, mount its connection string as a file and name the file:

```yaml title="docker-compose.yml"
services:
  stage:
    image: cratis/stage:latest
    environment:
      CRATIS_CHRONICLE_CONNECTION_STRING_FILE: /run/configs/chronicle-connection
      STAGE_EVENT_STORE: my-application   # a fixed name, or each new container invents its own store
    configs:
      - source: chronicle-connection
        target: /run/configs/chronicle-connection
    volumes:
      - ./model:/eventmodel:ro
configs:
  chronicle-connection:
    content: chronicle://client:secret@chronicle:35000
```

The container then starts no kernel and does not wait for port 35000. It keeps everything else: input validation, `--warm` and the warm handoff restart. The entrypoint prints the connection with the password masked and never the file's contents. This is the sandbox counterpart of the external kernel a [published application](../reference/published-applications.md) can use; unlike it, the sandbox keeps its development surfaces.

### Use the application

1. Open [session status](http://localhost:9090/stage/status) and
   [semantic admission](http://localhost:9090/stage/semantic/admission). Resolve refusals before treating the session
   as ready. A reachable host alone does not establish that your model can execute.
2. Open the [application UI](http://localhost:9090/). Stage serves authored screens or default per-slice Scene
   screens derived from admitted commands and read models. Use a supported command form, or open
   [Scalar](http://localhost:9090/scalar/v1) and submit the command there using its model-specific schema.
   Do not guess endpoint routes or payloads: the [OpenAPI document](http://localhost:9090/openapi/v1.json)
   describes the mapped operations. HTTP 501 or an unsupported result means that behavior did not execute.
   For the example above, use Scalar to select `GreetCustomer` and send `id` as
   `11111111-1111-4111-8111-111111111111` and `text` as `Hello from Stage`.
3. Run the corresponding query in Scalar, or open its screen, to inspect the changed read state. For the example,
   call `AllWelcomes` or open Welcome: after the accepted command, look for a row with that `welcomeId` and text.
   For an admitted command with event production, Stage evaluates the decision, records its accepted facts through Chronicle,
   and makes supported projected state available to queries. The
   [semantic read-model browser](http://localhost:9090/read-models) inspects the in-process views.
4. Open [Workbench through Stage](http://localhost:9090/workbench) to inspect the session's event history.
   For the example, look for `CustomerGreeted` on the event source you supplied.
   Workbench shows Chronicle events; use the semantic browser for Stage's in-process read models. Those are not
   Chronicle projection views. Storage is disposable, so this exploration does not establish persistence for a
   deployed application.
5. Check declared expectations separately with the
   [semantic specification runner](spec-runner.md#run-semantic-specifications). Read every reported outcome:
   failed and unsupported specifications are not passes. The small example above declares no specifications;
   add modeled expectations before expecting behavioral coverage. A ready host does not mean specifications have run,
   and model results do not establish correctness of a rendered application.

You have now exercised the model's supported command-to-state path without generating and building an application.
To develop source you own, continue with [Plan artifacts from Screenplay sources](../reference/plan-from-sources.md).

If startup refuses the model, read the diagnostics and compare the construct with the
[supported semantic subset](#opt-in-to-semantic-execution). Missing references and compile diagnostics need model
changes; runtime refusals identify behavior the selected engine cannot perform. Do not switch to the deprecated
`eventmodel` engine just to hide semantic refusals.

> **Published applications.** The sandbox above is for trying out a model. To run an application for real users, export a versioned package and start the image in published mode: persistent storage, sign-in by a trusted proxy, and none of the tooling surfaces. See [Published applications and export](../reference/published-applications.md).

## What is inside

The image is built **on top of the Chronicle kernel image** (`cratis/chronicle`), so the event store and the
engine performing the model live side by side and talk over localhost:

```mermaid
flowchart TB
    subgraph container["cratis/stage container"]
        direction TB
        Stage["▶️ Stage host<br/>/stage · port 9090<br/>Arc commands & queries · OpenAPI"]
        Kernel["📖 Chronicle kernel<br/>/app · port 35000<br/>event store · Workbench · API"]
        Model[("📄 /eventmodel<br/>*.play")]
        Model --> Stage
        Stage <-->|"gRPC · localhost:35000"| Kernel
    end
```

Three things about that base image are worth knowing:

- **The kernel is pinned to the version the host's Chronicle client is built against.** The Dockerfile names an
  exact `cratis/chronicle:<version>-development` tag — the same version `Directory.Packages.props` pins for the
  `Cratis.Chronicle` client — rather than `latest`. The two talk gRPC over a contract that changes between
  Chronicle versions, so letting either float is how a client and a kernel end up disagreeing about a message
  shape and the host fails at boot.
- **Storage is fully in-memory.** The MongoDB install the Chronicle image ships with is purged from the layer and
  the kernel is started with `Cratis__Chronicle__Storage__Type=InMemory`. The image carries no database, and
  every session starts empty.
- **The Stage host is a framework-dependent publish copied in, not compiled in the Docker build.** The
  `dotnet publish` output is produced on the host (by `dockerize.sh` or the publish workflow) and only copied
  into the image, because portable IL does not need to be recompiled per architecture.

## How it boots

`entrypoint-stage.sh` runs both processes in order:

1. **Check the selected input**, defaulting to `/eventmodel`. A missing path, a file without a `.play`
   extension, or a folder without `.play` files fails before the Chronicle kernel starts. File extensions are
   case-insensitive. This checks the input path, not the validity or executable capabilities of the model.
2. **Start the Chronicle kernel** from `/app` with in-memory storage, and with its API and Workbench features
   turned on explicitly.
3. **Wait for the kernel** — it polls port `35000` until the gRPC endpoint accepts connections, so the Stage
   never races ahead of the event store it needs.
4. **Start the Stage host** with the selected path. It compiles just that file, or compiles every `.play` file
   beneath the folder as one application using Screenplay's folder compiler. The host materializes the runtime
   constructs it currently supports. After the host starts, the default semantic path registers event contracts
   with Chronicle, seeds event sources that have no events yet, and reconstructs its in-process world from those
   facts. Its read models are held in Stage's process, not registered as Chronicle projections.

Each session gets a generated, Docker-style event store name (`brave-mendel`, `nifty-turing`, …), which is what
you will see in the log line the host prints and in the Workbench.

### Wait for the session to settle

A session passes through these phases before it can take commands, whether you start it with `cratis run` or
`docker run`:

1. **Image pull.** Only on first use of an image tag.
2. **Chronicle kernel boot.** The entrypoint waits until port `35000` accepts connections.
3. **Stage host start.** The host compiles the model and admits its runtime constructs. Port `9090` does not
   accept connections yet.
4. **Chronicle registration and world rebuild.** Port `9090` answers, and `GET /stage/status` reports
   `state: "loading"` while the host registers event contracts, seeds event sources and reconstructs its world.
5. **Settled.** `GET /stage/status` reports `ready`, or `unsupported` with the refusing constructs in `issues`.

As an example, a 120-slice model on half a CPU core booted the Chronicle silo in about 6 seconds, and the whole
container start took about 33 seconds. A first run adds the image pull to that. Your model size and machine change
these figures, so wait for the status instead of sleeping for a fixed time.

To wait for a settled session with one request, pass `wait` (in seconds) to the status endpoint:

```bash title="Terminal: wait for the session"
curl --retry-all-errors --retry 60 --retry-delay 1 -fsS 'http://localhost:9090/stage/status?wait=120'
```

The retry flags (curl 7.71 or later) cover the phases before port `9090` accepts connections, including the
empty reply a Docker port proxy can give while nothing listens behind it. Once it does, the host holds the request
while the state is `loading` and answers as soon as the state changes or the wait elapses, with the same JSON and
HTTP 200 as a request without `wait`. Read `state` in the response:

| `state`       | Meaning                                                                                                   |
| ------------- | --------------------------------------------------------------------------------------------------------- |
| `loading`     | The world is still being rebuilt and the wait elapsed first. Ask again; `/api/**` answers HTTP 503 for now. |
| `ready`       | The session can take commands and queries.                                                                |
| `unsupported` | The host refused the model. `issues` lists each refusing construct; `/api/**` answers HTTP 501.           |

Compare the constructs in `issues` with the [supported semantic subset](#opt-in-to-semantic-execution).
`wait` is capped at 300 seconds, a value that is not a whole number is rejected with HTTP 400, and without `wait`
(or with `wait=0`) the endpoint answers at once.

## Ports

| Port | Protocol | What it serves |
|---|---|---|
| `9090` | HTTP | The model's API — commands, queries, OpenAPI and the Scalar reference. See [URLs of a running Stage](../reference/urls.md). |
| `35000` | HTTPS | The Chronicle kernel: gRPC (HTTP/2) for the Stage's own client, plus the **Chronicle Workbench**, its API and OAuth (HTTP/1.1) on the same port. |

Those two are all a caller needs. The remaining ports are internal to the container and can be ignored: the kernel's
Orleans clustering on `11111` and `30000`, and the Stage's own Orleans silo on `21111` and `31000`.

### The Stage silo

The Stage hosts its own Orleans silo through [Cratis.Choreography](https://github.com/Cratis/Choreography), next to the
Chronicle kernel, so durable state machines and workflows have grains to run on. It uses the local configuration:
localhost clustering and in-memory grain storage, so it starts without MongoDB and nothing it holds survives the
container. Jobs, Chronicle-backed event-sourced state machines and the workflow engine are registered.

Set `Stage:Orleans:Storage` to `MongoDB`, with `Stage:Orleans:ConnectionString` and `Stage:Orleans:DatabaseName`, to keep grain state across
restarts. The ledger of [ability invocations](../reference/ability-invocations.md#durability-of-the-ledger) lives in grain storage, and Stage refuses
abilities that happen at most once while it is in memory.

The kernel is itself an Orleans silo in the same container and binds the Orleans default ports, so the Stage silo
listens on its own (`21111` silo, `31000` gateway) and joins a distinct cluster (`cratis-stage`). Override the ports
with `Stage:Orleans:SiloPort` and `Stage:Orleans:GatewayPort` in `cratis-stage.json` if something else in your
container needs them.

:::caution
**Port `35000` is HTTPS only — reach it as `https://localhost:35000`.** Kestrel can only serve HTTP/1.1 and
HTTP/2 on a single port over TLS, where ALPN negotiates the protocol per connection, so the kernel has no
plaintext mode for this port. A plain `http://` request to it gets no reply at all — `ERR_EMPTY_RESPONSE` in a
browser, `curl: (52) Empty reply from server` on the command line.

The certificate is a self-signed development one the kernel generates when none is configured, so a browser
warns the first time you open the Workbench, and command-line clients need `curl -k` (or the equivalent).
:::

## Mounting the model

The model is read from `/eventmodel` inside the container:

```bash
docker run --rm -p 127.0.0.1:9090:9090 -p 127.0.0.1:35000:35000 \
    -v /path/to/screenplays:/eventmodel:ro \
    cratis/stage:latest
```

The folder is searched recursively, so a model split across many `.play` files in nested folders works as-is.
`/eventmodel` is a plain directory rather than a declared volume, which means a model can also be pushed in
through the container archive API instead of a bind mount — that is how tooling that has no host folder to share
(Studio, for one) supplies a model.

To load a single file, mount only that file read-only and pass its container path after the image name:

```bash
docker run --rm -p 127.0.0.1:9090:9090 -p 127.0.0.1:35000:35000 \
    -v /path/to/invoicing.play:/eventmodel/input.play:ro \
    cratis/stage:latest /eventmodel/input.play
```

The entrypoint forwards absolute paths unchanged and resolves relative paths against its initial working directory
before starting the host. A single-file input does not discover sibling `.play` files. Implementation `file` references remain symbolic: this loader does not open or execute them, and
no mount of their parent folder is needed. Folder compilation resolves declarations across the discovered files
and reports compilation errors with relative source paths. Compilation alone does not establish that Stage can
execute every modeled construct.

### Serve static assets from the model

A layout's `logo image "/assets/logo.svg"` is a URL, so the file behind it has to be served. A sandbox session serves
the files in an `assets` folder next to the model, read-only, at `/assets/...`: with the model mounted at
`/eventmodel`, `/eventmodel/assets/logo.svg` answers `GET /assets/logo.svg`. For a single-file input the folder is
`assets` beside that file. Files in nested folders are served by their relative path, and the content type follows
the file extension.

Only files under `assets` are served; directories are not listed and a request that tries to leave the folder
answers `404`. A model without an `assets` folder serves nothing at `/assets`. A [published](#published-mode-preview) host does not
serve model files, so ship the asset with the application's own static web root.

## Configuration

The host separates **hosting** defaults from **deployment** configuration:

| Source | Carries |
|---|---|
| `appsettings.json` / `appsettings.Docker.json` | Hosting defaults only — the Kestrel endpoint (`http://0.0.0.0:9090`) and the correlation-id (`X-Correlation-Id`) and tenant (`X-Tenant-Id`) HTTP headers. |
| `cratis-stage.json` | Deployment configuration, loaded from the content root if present. Optional, and reloaded on change. |
| `STAGE_CONFIG` | Overrides the path `cratis-stage.json` is read from — point it at a mounted file to configure a session without rebuilding the image. |
| `CRATIS_CHRONICLE_CONNECTION_STRING_FILE` | The path of a mounted file holding the connection string of a Chronicle kernel that is already running. When it names a readable, non-empty file the session uses that kernel and starts none of its own, so the event log can live in a database and outlive the container. An unreadable or empty file exits with code 78. See [Use an external Chronicle kernel](#use-an-external-chronicle-kernel). |

Anything Chronicle-related can also be set with the kernel's own environment variables
(`Cratis__Chronicle__…`), which is how the entrypoint pins in-memory storage and the Workbench.

## Legacy-engine command validation

With `Stage__Runtime__Engine=eventmodel`, the legacy EventModel engine enforces this subset of
command-level declarative `validate` rules:

| Rule | Legacy-engine behavior |
|---|---|
| `not empty` | Rejects missing or null values, blank text and empty collections. Nested object paths such as `customer.address.name` are resolved through the payload. |
| `min` / `max` with a numeric literal | Inclusive value bounds on numbers (including decimals); inclusive UTF-16 length bounds on text. |
| `>` / `>=` / `<` / `<=` with a numeric literal | Compares numeric values or text's UTF-16 length against the threshold. |
| `length` with a non-negative integer literal up to 2147483647 | Requires that exact text length. |
| `matches` with a quoted literal | Applies the literal as a .NET regular expression. |

Except for `not empty`, these rules allow absent or null values. Authored messages are retained.
Rules on paths that cross a collection are skipped, including `not empty` on `lines.description`;
scalar rules cannot express element validation. Use `lines not empty` to require a nonempty collection.
Numeric bounds and comparisons skip values that are neither numbers nor text (booleans, objects and collections).

The legacy engine **drops**, rather than partially enforces, `all >` and `all >=`, equality and
inequality (`==` / `!=`), checksums, ordinal text rules (`contains`, `not contains`, `starts with`,
`ends with`), named rules, inline code validation, and rules with non-literal
operands (including property references, `today` and named match patterns such as `email`). It does
not turn a collection quantifier into a scalar comparison. Dropped rules do not reject a command;
compilation alone does not mean the legacy engine enforces all modeled validation. Use semantic
execution for its admitted rule forms and inspect its admission report for unsupported constructs.
An unknown rule kind refuses the model.

## Opt in to semantic execution

The host uses the semantic engine by default. To keep the legacy EventModel behavior, set
`Stage__Runtime__Engine=eventmodel` or pass `--engine=eventmodel` to the Host executable.
The EventModel engine is **deprecated**: it logs one warning at startup and will be removed in Stage v6.
The SpecRunner retains `structural` as its default; this Host default does not change its report format.
The legacy engine refuses named event sources, streams
and event-source routes with `STAGE-ESM-030`, before registering anything with Chronicle. The host stays
in refused mode: `/stage/status` reports `state: "unsupported"` and `/api/**` returns HTTP 501.
The semantic sandbox reports public events and public-event projections (`STAGE-ESM-032`), foreign events
and `source events` captures (`STAGE-ESM-033`), directed Translate slices and captures (`STAGE-ESM-024`)
as `renderedAround` entries on `/stage/semantic/admission`, with a startup warning for each artifact. They do
not block unrelated local commands or queries. Public events are **published to the outbox**: a local public event is registered as public and appended to the
application's own outbox, never to its event log, including supported outbound event projections. Another
application receives it through its inbox when the two run against the same Chronicle kernel. Foreign
events are registered with the store they come from, and the application's store is subscribed to that store's
outbox: what arrives in the inbox joins the world, sets off its captures and reactions, and what they append reaches
the event log. The origin store need not be running first: Chronicle creates it when the foreign event is registered and starts delivering once the application that owns it publishes, so nothing arrives from an origin until that application runs against the same kernel. Run the applications of a project together, pinning each one's event store name (`STAGE_EVENT_STORE`) to the name its consumers write after `from`. Foreign events also still arrive through seeds, and captures do not poll external sources. A restart
does not replay the inbox into the world, so start a fresh session after restarting a running host. Unsupported public-projection shapes still refuse the affected command without appending facts.
Because the event log holds no publication, a restart over a log is rebuilt without the former `World rebuild refused` stop for local publications.
The legacy engine continues to refuse these constructs. Both engines refuse reducer observer filters
(`STAGE-ESM-035`). The legacy engine also refuses reaction execution identity
(`STAGE-ESM-034`) and reaction observer filters (`STAGE-ESM-035`). Semantic rendering supports reaction filters; neither that support nor the renderer's narrowed `034` refusal for claim-reading or opaque gates changes these legacy-engine refusals.
The semantic engine runs per-production route overrides; it does not admit an override on an unkeyed stream under a keyed command route, or a literal override stream id of `Default`, yet; both fail with `STAGE-ESM-036` until their generated routing is proven equivalent. The legacy engine refuses the stream syntax with `STAGE-ESM-030`.
The semantic engine runs event-triggered reactions, including `where`, `runs as`, `from`, productions,
invocations and cascades. It settles them with the initiating command and appends all facts in one
tail-guarded batch. A rejected, denied or unsupported settlement refuses the whole command and appends
nothing, retaining the previously committed world. With `reaction batches atomic`, each trigger occurrence's
direct productions and each selected refusal branch are atomic units during evaluation; invoked commands
remain separate units. The live Host still rolls back the **whole initiating command and its cascade** if
any unit fails, rather than keeping earlier successful units as the reference evaluator does. Models without
the declaration retain legacy per-fact reaction evaluation and the same Host rollback boundary.
This differs from rendered Chronicle reactors, which run eventually and retry failed partitions.
Clock, application, startup and shutdown triggers and opaque reaction bodies still fail `STAGE-ESM-024`
naming the trigger kind; unsupported context mappings such as `$context.causedBy.*` fail `STAGE-ESM-013`.
The independent specification executor also settles event reactions, including identity, filters and mixed effects,
but runs only the slice that declares each specification: other slices' reactions and publications do not run, and an
invoked command another slice owns is decided without keeping its events. See [The spec runner](spec-runner.md#a-specification-runs-its-own-slice-only) for this and its scenario limits.
The semantic engine checks each language/semantic version pair against Stage's declared ESM support. Audited
pairs and newer pairs within its declared range proceed to member and capability checks; a version alone does
not establish support for every construct. Pairs outside that policy and unknown ESM members refuse the model
with `STAGE-ESM-016` before registration or execution. Check `/stage/semantic/admission` for the loaded model's
capability refusals.
A v22 model that uses `reaction batches atomic` executes atomic production units while retaining the Host's whole-command rollback boundary.
At v28 the semantic engine runs Uuid `==` and `!=` in command `require` conditions and `produces when` guards, including
through concepts and protected read paths such as `require Grant.memberId == requestedBy`. Uuids compare ordinally in their
canonical lowercase hyphenated spelling, and the Host rejects a non-canonical Uuid input (uppercase, braced or unhyphenated)
before any requirement runs. The renderer renders property, literal and protected read-path comparisons in `require` as Guid comparisons; a read
path in `produces when` keeps `STAGE-ESM-044`. A rendered application binds Uuid input through Arc, which also accepts
uppercase or braced spellings. The legacy engines refuse every Uuid comparison with `STAGE-ESM-049`.
At v27 the semantic engine runs read models with several `key` parts: instances that differ in one part stay apart,
`parts` projection keys create and remove only the instance whose parts all match, list queries filter on any part,
and a single-instance query by parts takes one parameter per part, validated against its concept like a single key.
Such a read model has no conventional `Get<ReadModel>ById` query, and the read-model browser shows each key as an
object keyed by part name. The renderer emits such a read model with a synthetic `Id` key record holding the parts and a
`UsingCompositeKey` projection, and refuses shapes it cannot express (reducers and flat projections, joins, children, `every`,
optional, date-time, decimal (Chronicle#5427) or non-primitive parts, incomplete `parts` keys) with `STAGE-ESM-048`. It refuses a single-instance query by parts with `STAGE-ESM-047` until
Chronicle can look up an instance by a composite key (Chronicle#4265); the legacy engine refuses the read model with
`STAGE-ESM-048`.
At v26 the semantic engine executes Date/DateTime ordering and instant equality in command requirements,
conditional productions and validation rules, including concept-backed properties. Participating DateTime inputs
must carry an explicit offset or `Z`; validation rejects missing offsets after authorization and before other
input validation, including reaction invocations. Date compares calendar days; DateTime compares UTC ticks.
DateTime event and read-model assertions instead compare ordinally after both sides use Screenplay's round-trip canonical text: zero offsets become UTC `Z`, while non-zero offsets remain distinct.
Specification expectations remain ordinal. Opaque `validate csharp` is an explicit ordering difference:
the specification executor refuses it at run admission, and the Host refuses it before authorization and
input validation. The reference checks authorization, then offsets, then opaque validation, so an offset-less
DateTime combined with opaque validation returns `Validation` in the reference but `Unsupported` in either
Stage path. `Unsupported` never counts as a pass. The renderer renders these comparisons (see
[Stage rendering](../reference/stage-rendering.md)); the legacy engines refuse them with `STAGE-ESM-046`.
The semantic engine executes `command reads protected`: after authorization and property/concept rules, each
required view must exist at its declared key. Absence rejects validation with `Current '<key>' does not exist.`
(using the view's name), before requirements or generation. All requirements then run, including read-backed
conditions; produced-event mappings can read the stored view values. `/validate` also reports required absence.
Protected commands with non-Error requirement severities remain refused with `STAGE-ESM-045` until Screenplay defines their severity semantics.
This includes reaction-invoked commands: model admission and the runtime refuse before evaluating reads or requirements or appending facts.
The Host decides against the session world, reads outside facts through the current log tail, then commits
against that extended world. Only a later fact matching a protected read's key and event contracts conflicts;
the command returns a concurrency violation without faulting the session. Unrelated facts remain in the
commit-time world, where constraints and projections run. Outside facts do not trigger reaction settlement again.
The complete root and reaction batch is written atomically against the durable pre-batch tail, including a
guarded empty write for a successful command with no facts. If that guard definitively rejects the write,
the Host can retry the same decision, with its generated values unchanged, only if settlement invoked no
commands. An invoked command prevents automatic retry; the operation returns a concurrency violation.
Repeated tail movement also returns a conflict after eight attempts. An unknown write outcome is never
retried or reported as success: the session faults until it can be reconstructed. Unreadable or unsupported
outside history also faults the session. Legacy custom appenders without fact-reading or guarded-write
capabilities retain their conservative safety checks.
The Cratis renderer renders protected reads as Arc read protection (see [Protected reads rendering](../reference/protected-reads-rendering.md)),
refusing only the shapes it cannot preserve with `STAGE-ESM-044`. The Contracts `EventModelLoader` admits the preamble and records it as `EventModel.ProtectedCommandReads`; the legacy syntax renderer, the `eventmodel` Host engine, the structural runner and `StageCommandHandlerProvider` refuse such a model with the same code. Without the preamble,
command reads still fail binding with `PLAY0271` and the repair diagnostic `PLAY0786`.
ESM v11 preserves retained exact Double-mode numbers in command decisions, appended payloads and
read-model state, including ±9007199254740991 and `0.10000000000000002`. Source integers still use
the Double safe range; `numbers exact` and arbitrary-precision arithmetic are not admitted.
ESM v12 preserves explicit enum member numbers when rebuilding stored integer values; decisions still compare member names.
ESM v13 admits concept compliance without changing command decisions or world reconstruction.
ESM v14 equality query filters execute through the reference evaluator in the Host, including
optional omission and required-filter rejection. The independent specification executor and Host
specification admission support equality-filtered assertions over projected and seeded read models.
Supplied scalar primitive, concept and enum filters combine with `by` using AND; omitted optional
filters do not narrow. Collection expectations compare rows by read model and key, not order.
Unrepresentable projection shapes still receive typed refusals. The executor supports complete scalar per-key seeds,
replacing projected Given history for that key before folding subsequent facts; see [Spec runner](spec-runner.md).
The semantic Stage Host encrypts personal-data and secret concept values at rest through Chronicle,
like rendered applications, and releases them on read. Personal-data erasure uses Chronicle's
key deletion keyed by the event source; explicit subject lineage remains
[Stage #28](https://github.com/Cratis/Stage/issues/28). Existing Play stores retain earlier plaintext events.
The semantic Host and renderer admit text and non-text `secret` concepts (`Int`, `Decimal`, `Bool`,
`Date`, `DateTime`, and `Uuid`), preserving their primitive types. `pii secret` uses PII only.
The renderer refuses compliance on identity uses or enums with `STAGE-ESM-040`;
see [Concept compliance rendering](../reference/compliance-rendering.md).
The semantic engine executes command routes, registers named sources and streams with Chronicle on every
connect, and checks stored routes during world reconstruction. Routed specification runs are still refused
with `STAGE-ESM-030`, as are routes in the structural SpecRunner and EventModel engine.
The default engine uses the Screenplay executable semantic model (ESM) for command decisions and keyed
snapshot queries. `Stage__Runtime__Engine=semantic` selects it explicitly:

```bash
docker run --rm -p 127.0.0.1:9090:9090 -p 127.0.0.1:35000:35000 \
    -e Stage__Runtime__Engine=semantic \
    -v /path/to/screenplays:/eventmodel:ro \
    cratis/stage:latest
```

The semantic engine evaluates validation, requirements, authorization, conditional production and constraints
before accepting a command. Accepted facts are appended to Chronicle as one batch; a failed append does not
commit the in-process read-model state. `GET /stage/semantic/admission` lists the model's admitted and
unsupported artifacts, state machines and workflows included; `GET /stage/lifecycles` reports where state machines are and what
workflows wait for (see [State machines and workflows](../reference/state-machines-and-workflows.md)). `GET /stage/invocations` lists ability invocations and the ones stopped for reconciliation (see [Durable ability invocations](../reference/ability-invocations.md)). `GET /stage/status` includes `engine: "semantic"`. If Screenplay cannot compile an
executable plan, the host returns `state: "unsupported"` with issues; `/api/**` responds with HTTP 501
instead of performing a partial model. Only Error-severity compiler diagnostics become blocking
`Plan` issues. Information and Warning diagnostics are returned separately in the optional `notes`
array on `GET /stage/status`, for both successful and refused loads. Each note preserves its `code`,
`message`, root-relative `source` location and `severity`; these notes do not prevent execution. An exit-42 warm
handoff restarts only Stage, retaining the Chronicle kernel and its in-memory event log. Other host exits stop
the container; replacing the container loses that log. An external or persistent Chronicle store can retain
history across container replacement, but that storage path is not verified by the sandbox.
With a nonempty log, Stage reads the complete event history,
checks its continuity, event schemas, origins and modeled constraints, then reconstructs the in-process
world using Screenplay semantics. It refuses incomplete or inconsistent history rather than starting
with partial state. `/stage/status` reports `loading` during rebuild; `/api/**` is unavailable then. To wait for
the rebuild to finish, see [Wait for the session to settle](#wait-for-the-session-to-settle).

Commands retain their slice-qualified `/api/…` route and legacy alias. The semantic engine also exposes one
query by its modeled name. Compatibility `Get…ById` and `All…` queries are available only for read models with at least one keyed query and no authorization on any keyed query targeting that read model. Otherwise, the compatibility routes are not mapped, so callers cannot bypass a modeled query policy.
Unknown or revised events, schema mismatches and unverified historical constraint claims cause
`Unsupported(World)` with a specific issue rather than a partial world. A logged event contract with a DateTime property also refuses because Chronicle
storage does not preserve these values exactly (this is a known limitation; the startup warning `STAGE-ESM-080` names the event). A decimal property restarts exactly when the
event type's stored schema carries Chronicle's exact `decimal` format on every number: Stage registers a new event type that way, and reads the stored schema back
after registration to decide. An event type that was first registered by an earlier Stage, or by another store, keeps its stored shape (the kernel refuses to change it), so a
restart over such a log refuses the decimal with a clear message, and startup warns with `STAGE-ESM-079`; use a new event type to get exact decimals. Stored seed events whose
schema is not exact are accepted only when the model's seed still equals them exactly. When two Stage instances register the same new event type at the same time, the
first registration wins and decides the shape; the kernel offers no atomic registration. The read-model sink narrowing is unchanged. Null optional event values, non-text destination IDs,
changed `causedBy.name` for the same subject, and duplicate tags can also fail the exact-history check.
Origin verification compares modeled tags and mapped `$context` values; it does not independently
verify literal mappings, production conditions, or that a destination matches a mapped event property.
Do not treat a successful rebuild as proof that another client could not append a plausible event.
After rebuild, an external append detected before a command faults the runtime instead of committing
against stale state. The semantic engine does not register Chronicle projection mirrors: its read models
are available through Stage's in-process queries, not as projection views in the Chronicle Workbench.
For admitted, unrouted models, the legacy EventModel engine continues to register its own Chronicle projections. Commands that require
allocation of a new event-source identity return `Unsupported(IdentityAllocation)`; explicitly name a
`produces … for` destination to make them executable. Unsupported event revisions or duplicate event names
put the running host in refused mode (`state: "unsupported"` and HTTP 501 for `/api/**`). When scene loading succeeded, `/stage/scene`, `/stage/routes` and `/stage/locales` remain available even in refused mode. The semantic engine runs modeled specifications through the shared executor on request, with fresh isolated state rather than the live world or Chronicle log (see below). Scene synthesis still uses the legacy presentation converter, but that converter is
never registered as the semantic execution model. A screen can therefore omit a semantic command even though
its API route exists. The in-process world holds the session's fact history without a size cap; use this
engine for short-lived play sessions, not durable workloads.

A denied command responds with HTTP 403. Failed validation and constraints respond with HTTP 400; a
constraint result includes its name in `reasonDetail`. An unsupported command responds with HTTP 501 and
`Stage-Unsupported-Capability` and `Stage-Unsupported-Artifact` headers. `/validate` evaluates the same
model without appending facts. Semantic queries require a modeled snapshot lookup; the compatibility All/ById
queries read the in-process world, not Chronicle projections. If an append's outcome cannot be established, Stage faults the session and returns `Unsupported(World)` for subsequent commands and queries; `/stage/status` then reports `unsupported`. Restart rebuilds from the Chronicle log only when its
history passes the same strict admission checks.

### Sandbox identity

The Docker sandbox enables `Stage:Sandbox:Identity` so a browser can exercise role-gated models without a
login screen. The setting is **off when absent** outside Docker. It supplies a server-selected persona for
`/api/**` requests and the page identity at `/.cratis/me`. `/stage/**` endpoints and the Chronicle Workbench
do not receive that persona.
This changes only the Stage host, never rendered applications or their authentication.

Stage decides for itself whether it is in development (see [Development](#development)); it does not read that
from `ASPNETCORE_ENVIRONMENT`. The sandbox is development, so Arc's discovery catalogs (`/.cratis/users`,
`/.cratis/tenants`) answer anonymous requests. The persona is not applied to these endpoints, and Play does not use them.

Stage selects the default persona once at startup:

1. Use the persona named by a `Cratis.Blueprint.Default.userMenu` component's literal `role` property.
2. Otherwise, use the persona whose policies satisfy the most module-level `authorize` requirements, in
   declaration order when tied.
3. Otherwise, use the first declared persona. With no personas, leave the caller anonymous.

The same model selects the same persona and claims across fresh or warm sessions. The persona receives
roles and literal claims synthesized from its declared policies, not every role in the model. Stage skips
policies it cannot synthesize from roles and claims, including anonymous requirements or conflicts with
previously accepted policies, and logs a warning. The audit subject is `persona:<name>`; the display name
comes from the user menu's literal `name` property, or the persona name. Startup logs identify the persona
and its roles. A persona lacking a required role still receives `403` for that operation.

Arc's forwarded-header trust remains off. In sandbox mode, Stage removes every incoming header whose name
starts with `x-ms-client-principal` (any letter case) from every request, before authentication, static files
and routing run. It also replaces any
incoming principal with the server-selected persona, or an anonymous principal when the model has no
personas. Sending headers or an existing authenticated identity cannot select another persona or add roles.
To use another persona, change the modeled user menu's `role` and restart the session.

:::danger[Sandbox identity is not production authentication]
Every sandbox API caller acts as the selected persona. There is no login or per-user identity verification.
Restrict access at a trusted proxy and keep the session private. This mechanism is only for exploring a
modeled application, not for protecting a production system.
:::

### Development

Stage owns its definition of development and tells ASP.NET Core and Arc about it, so a deployment's
`ASPNETCORE_ENVIRONMENT` does not by itself turn on Stage's development surfaces.

- The play sandbox is development unless its host environment is explicitly `Production`.
  Set `Stage:Development` (`Stage__Development=false`) to turn that off, or explicitly set it to `true` to override the environment.
- A published application is never development, and no setting makes it so.

Development is what lets surfaces meant for tools open: Arc maps its user and tenant discovery endpoints
anonymously, and the sandbox may offer modeled users. It does not turn any of them on by itself.

### Development users

A model can declare the people a session acts as with `user` (see [Personas](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/personas.md#users)):

```screenplay
persona SchemaSteward
  policy NamingSchemaAdmin

user kari "Kari Nordmann" as SchemaSteward
user guest "Guest"
```

Set `Stage:Sandbox:Users` (`Stage__Sandbox__Users=true`) to offer them. The switch is explicit and off when absent.
The selected user is only accepted from the local machine with a Host header of `localhost`,
`127.0.0.1`, or `[::1]` (with any port) unless `Stage:Sandbox:UsersAllowRemote`
(`Stage__Sandbox__UsersAllowRemote=true`) is set. Stage uses the connection address, not `X-Forwarded-For`.
Inside a container, traffic from the host is not loopback, so Docker needs the remote-access opt-in:

```shell
docker run --rm -p 127.0.0.1:9090:9090 -p 127.0.0.1:35000:35000 \
  -e Stage__Sandbox__Users=true -e Stage__Sandbox__UsersAllowRemote=true \
  -v "$PWD":/eventmodel:ro cratis/stage:latest
```

- The Stage browser topbar offers **Acting as** when the sandbox has modeled users. Pick a user to make actual commands,
  queries, and observable subscriptions run as that user. The page reloads after a successful switch so identity details and
  subscriptions are recreated together. **No acting user** clears the selection and leaves browser calls anonymous.
- Stage lists the users at `/.cratis/users`, with the roles and claims synthesized from the policies of the personas each one
  plays. Lens reads that list and can still inject a caller independently; explicit identity headers take precedence over the
  browser selection.
- The single server-selected persona above is off while users are on, and Stage trusts the identity headers the tool sends.
- Stage refuses to start if `Stage:Sandbox:Users` is on when Stage is not in development, or together with
  `Stage:Sandbox:ForwardedIdentity`. A published application never offers users.

The browser selection uses an HttpOnly, SameSite=Strict session cookie containing only the modeled name. Stage resolves its
roles and claims from the current model on each request; stale names are ignored and cleared. The picker and cookie mapping
are unavailable in Production, published mode, or without the explicit users switch and a nonempty modeled user list.
This is a real sandbox caller, not a design-time persona preview or a production sign-in mechanism.

The cookie belongs to the sandbox's host and path, not its port. Separate localhost sessions can therefore share a name;
Stage accepts it only if the current session models that user. Unsafe requests and WebSocket upgrades need a same-origin
Origin or Referer to use the cookie; changing the selection requires a same-origin Origin and JSON. A different localhost
port is not the same origin.

Behind a path-prefixed proxy, preserve the sandbox's base path and cookies. The selection cookie is scoped to `PathBase`
(`/` when absent) and uses Secure on HTTPS. If the proxy strips cookies, the picker cannot persist a caller. Generated
applications and static sources do not offer this capability. For the HTTP contract, see
[Sandbox acting-user selection](../reference/urls.md#sandbox-acting-user-selection).

:::danger[Modeled users are not authentication]
Local callers, and remote callers when remote access is allowed, can present any identity, including a role the model never
gave a user. Keep the session private, and leave `Stage:Sandbox:Users` off anywhere that is not a development or test environment.
:::

To disable the persona middleware, set `Stage__Sandbox__Identity=false` in the host environment (for Docker,
add `-e Stage__Sandbox__Identity=false` to your container command). You can also override the setting in the
Stage configuration file:

```json
{
  "Stage": {
    "Sandbox": {
      "Identity": false
    }
  }
}
```

With the setting disabled, Stage neither injects a persona nor strips headers or replaces an existing
principal. Arc's default forwarded-header trust is still off. An anonymous semantic caller has empty audit
identity fields (`subject`, `name`, `userName`); the legacy EventModel path omits those values entirely.

### Browse read models in a semantic session

The semantic engine keeps read models in its live in-process world, not in Chronicle projections. To see what
your commands produced, open `http://localhost:9090/read-models` (or `<session-base>/read-models` behind a proxy,
for example `/api/play/<session>/read-models` in Studio). The browser lists every read model, including ones
without queries or instances. Select a model to inspect its keys and property values; arrays and composite
values appear as compact JSON. **Refresh** reads the current world again, including the selected model.
The Chronicle Workbench still shows the event log, but does not show these semantic read models.

You can also read the same state over HTTP:

- `GET /stage/read-models` returns the models in declaration order, their module/feature/slice location,
  property names and types, and current instance counts.
- `GET /stage/read-models/{id}` returns one model's property contract and current instances. Use the `id`
  from the list; an unknown or malformed identity returns HTTP 404.

For example, a list entry has this shape (the semantic identity is abbreviated for illustration):

```json
{
  "readModels": [{
    "id": "sm:…",
    "name": "ProjectSummary",
    "module": "Projects",
    "feature": "Registration",
    "slice": "ProjectLookup",
    "properties": [{ "name": "name", "type": "ProjectName" }],
    "instances": 1
  }]
}
```

Inspecting that model returns named JSON values, not semantic property identities:

```json
{
  "id": "sm:…",
  "name": "ProjectSummary",
  "properties": [{ "name": "name", "type": "ProjectName" }],
  "instances": [{ "key": "project-one", "values": { "name": "Screenplay" } }]
}
```

Both endpoints return HTTP 503 with `state: "loading"` and `Retry-After: 1` until the world has been rebuilt.
A faulted session returns HTTP 503 with `{ "state": "faulted", "reason": "…" }` and no rows, rather than
presenting indeterminate state as current. Restart the session before inspecting it again. A refused model
returns HTTP 501 with the same refusal headers and `exceptionMessages` as the specification endpoint.
These diagnostic endpoints expose the whole session, independently of modeled query authorization; keep the
session private behind the trusted proxy described above.

Under the `eventmodel` engine, both endpoints return HTTP 404 with `engine: "eventmodel"` and a `reason`
explaining that read models are shown in the Chronicle Workbench. The browser displays that message instead
of a read-model table.

### Run specifications in a semantic session

Post to `POST /stage/semantic/specifications/run` with an empty object or an empty `scopes` array to run all specifications:

```bash
curl -X POST http://localhost:9090/stage/semantic/specifications/run \
    -H 'Content-Type: application/json' -d '{"scopes":[]}'
```

To select a subset, supply semantic identities in `scopes`. Application, module, feature, slice and specification
identities use the same selection rules as the [semantic specification runner](spec-runner.md#opt-in-to-semantic-execution).
The response uses its existing `stage-spec-run/1` report schema. HTTP 200 means a report was returned, not that
every specification passed: inspect each `outcome` (`Passed`, `Failed`, `Unsupported` or `Cancelled`). Refusals
carry `unsupported.capability`, `unsupported.construct` and `unsupported.details`. An unknown scope produces
an `Unsupported(Specification)` record rather than being ignored; a malformed identity returns HTTP 400 without
running any specifications. Duplicate scopes are selected once; more than 1000 scopes returns HTTP 400.
A model refused at initial compilation or runtime admission returns HTTP 501 with the host's refusal headers and issues.
If an admitted model's live world rebuild or Chronicle registration fails later, specifications remain available:
they depend only on the admitted model, not that live world or registration.

The host launches the semantic SpecRunner as a separate process. Each specification runs against a fresh
in-memory Chronicle event log and Arc scenario. Runs never seed from, append to, or modify the session's live
event store or world. Testing and kernel assemblies are loaded only by the child, never by the Host. Runs are
serialized process-wide. Request cancellation kills the child process tree; a disconnected caller receives no
completed report. A timeout returns HTTP 504; launch failure, nonzero exit, or a missing/invalid report returns
HTTP 502 problem details, never a passed report. The report must match the host's admitted model revision;
changing source files after startup requires restarting the host before they can be tested.

`dotnet publish Source/Host/Host.csproj` includes the runner in `specrunner/`. Configure
`Stage__Specifications__RunnerPath` to override the default `specrunner/Cratis.Stage.SpecRunner.dll` beneath the
host output, and `Stage__Specifications__TimeoutSeconds` for the run deadline (default 120, clamped to 1–600
seconds, including queued time). The same keys can be set in deployment configuration as
`Stage:Specifications:RunnerPath` and `Stage:Specifications:TimeoutSeconds`.

Named C# rules and C# validation iterators on commands and their input concepts run in a separate body worker.
The Host compiles admitted units once at startup and shares one execution worker across command commits for
that loaded application. Specification runs compile once in the runner and share its execution worker across
specifications. Each commit or specification gets a lightweight provider; releasing it does not kill the worker.
Unloading the application or exiting the runner disposes the shared worker. Neither process loads the units
into the Host. The Host forwards its worker settings to SpecRunner, so the same limits apply to both paths.

An admitted validation iterator yields zero messages to accept, or rejection messages in declaration order.
Messages are limited to 100 per iterator and 1024 characters each; null messages, exceeded bounds and iterator
failures report a worker failure, not a successful validation. Query-argument code validations, command
handlers and reducer transitions remain unsupported.

Keep the Host, SpecRunner and BodyWorker from the same Stage build. Worker protocol version 2 rejects older
workers during the initial handshake, before sending a request.

| Environment variable | Default | Range or resolution |
| --- | --- | --- |
| `Stage__BodyWorker__Enabled` | `true` | Set `false` to keep implementation bodies unsupported. |
| `Stage__BodyWorker__WorkerPath` | `bodyworker/Cratis.Stage.BodyWorker.dll` | Relative to the Host output directory. |
| `Stage__BodyWorker__CallTimeoutMilliseconds` | `2000` | 50–30000 ms per evaluation. |
| `Stage__BodyWorker__CompileTimeoutSeconds` | `120` | 5–600 seconds. |
| `Stage__BodyWorker__MemoryLimitMegabytes` | `256` | 32–4096 MB managed heap; working-set watchdog at twice this value. |
| `Stage__BodyWorker__CacheDirectory` | `Cratis/Stage/Bodies` beneath your local application-data directory | Relative overrides resolve against the working directory. The cache must be owned by the current user and protected from other users. |

The cache holds executable assemblies, so do not share it between operating-system users. On Unix,
Stage creates new cache directories with mode `0700` and uses `/usr/bin/stat` (or `/bin/stat`) to check
ownership. It refuses foreign-owned or group/world-writable caches, symbolic-link cache roots and
replaceable parent directories; root-owned sticky temporary parents are allowed. Windows checks ownership
and effective write/delete permissions, including inherited permissions on the cache itself. Inherit-only
ACEs and CREATOR OWNER / CREATOR GROUP templates do not grant access to the directory being checked.
An unsafe cache produces an explicit worker failure, not cached execution; Stage does not repair its
permissions. If local application data is unavailable, the default is a private, user-suffixed directory
beneath the system temporary directory.

**macOS limitation:** Unix validation checks ownership and POSIX mode bits, not extended ACL grants.
Mode `0700` alone is not proof of isolation when a named ACL grants another user access. In a hostile
multi-user environment, use a cache directory and ancestors with no effective extended ACL grants that
let other users write, delete or replace the cache or its contents; Stage does not verify this condition.

Stage rechecks cache ownership, permissions and ancestors before compilation and every evaluation.
On Unix, each check starts a metadata process, so short, frequently evaluated bodies incur that overhead.
These checks are not memoized: permissions or ancestors can change after startup or compilation, and
reusing an earlier verdict would weaken the execution-time check.

These keys also work as colon-separated deployment configuration, for example `Stage:BodyWorker:Enabled`.
For a standalone semantic SpecRunner, supply `--body-worker-path` and optionally `--body-worker-cache`,
`--body-worker-dotnet`, `--body-worker-timeout-ms`, `--body-worker-compile-timeout-ms`,
`--body-worker-heap-limit-mb`, `--body-worker-working-set-limit-mb`, `--body-worker-max-response-bytes`
and `--body-worker-max-stderr-bytes`. `BodyWorkerOptions.FromArguments` parses this same argument contract;
without worker arguments it returns no configuration and body rules remain unsupported.
A wall-time breach reports `STAGE-IMPL-007`, a memory breach `STAGE-IMPL-008`, and an unavailable or failed
worker `STAGE-IMPL-009`. Each kills the worker; a later run can start a fresh one and the Host keeps serving.
The managed heap limit plus working-set watchdog is not an OS memory cap or a hostile-code security boundary.

`GET /stage/semantic/admission` marks
specifications admitted by that executor as `supported`, and reports the missing capability and details for
those it refuses, without blocking the live model. The legacy EventModel engine does not expose this route.
The `/stage/status`, `/stage/scene` and runner `results.json` schemas are unchanged.

### Replay recorded journeys

When a model is derived from an existing system, you want to know whether it behaves like that system. Record
what the system did as journeys: ordered commands and queries with the answers the system gave. Then replay them
against the running sandbox. Each step is addressed by its model name and sent over the session's own `/api`
routes, so binding, validation, authorization, persona selection and serialization are the ones a browser sees.
[Specification runs](#run-specifications-in-a-semantic-session) check the model against its own specifications;
a replay checks it against behavior recorded outside the model.

A journey file uses the `stage-journey/v1` schema:

```json
{
  "schema": "stage-journey/v1",
  "journeys": [
    {
      "name": "register a project twice",
      "note": "Recorded from the original portfolio service.",
      "steps": [
        { "name": "register", "command": "Portfolio.RegisterProject",
          "input": { "projectId": "$new:project", "name": "Apollo", "startsOn": "${today+7d}" },
          "bind": { "registered": "$.response.projectId" } },
        { "name": "listed", "query": "AllProjects", "within": 2000,
          "expect": { "result": [{ "projectId": "${registered}", "name": "Apollo" }] } },
        { "name": "again", "command": "RegisterProject",
          "input": { "projectId": "$new:other", "name": "Apollo" } }
      ]
    },
    {
      "name": "no archived projects",
      "steps": [{ "query": "ArchivedProjects", "expect": { "empty": true } }]
    }
  ]
}
```

Post it as the request body:

```bash
curl -X POST http://localhost:9090/stage/semantic/journeys/replay \
    -H 'Content-Type: application/json' --data-binary @journeys.json
```

The body must be sent as `application/json`; any other content type is refused with 415. Add `?journey=<name>`
(repeatable) to replay only the named journeys.

A step names exactly one `command` or `query`, as a qualified `Namespace.Name` or a simple name. A command takes
its body as `input`; a query takes flat string, number or boolean `arguments`, sent as the query string. A query
is addressed by its simple name: a namespace in front of it is not checked. A name that is unknown, or a simple
name that several operations share, makes that step an `error` instead of guessing.

`expect` describes the answer. `outcome` is `accepted` (the default), `rejected`, `unauthorized` or
`unsupported`. With a refusal outcome, `messages` lists texts that some validation, authorization or refusal
message must contain, ignoring case. With `accepted`, `response` is matched against the command response,
and `result` against the query data. `empty: true` requires a query to return no rows, and `absent` lists values
that no row may match. A query step without `expect` only has to succeed.

| Rule | Behavior |
| --- | --- |
| Objects | Every expected property must be present; extra properties are ignored and names compare ignoring case. |
| `null` | An expected `null` matches `null` or a missing property. |
| Scalars | Strings compare exactly, numbers by value (`1` equals `1.0`); a string never matches a number. |
| `list: "contains"` (default) | Each expected element matches a different actual element, in any order. Elements are paired with the first unused match, in expected order. |
| `list: "ordered"` | Expected elements appear in the same relative order; other elements may sit between them. |
| `list: "exact"` | Same length, and element *i* matches element *i*. |

The `list` setting applies to every array under that step's `response`, `result` and `absent`.

Recorded identities rarely fit a fresh session, so strings in `input`, `arguments` and `expect` can carry
bindings. `"$new:<name>"` generates a GUID the first time a journey mentions it and reuses it afterwards.
A step's `bind` captures values once the step passes, from `$.response...` for a command or `$.result...` for a
query, using `.name` and `[index]` segments. `${<name>}` substitutes a bound value: a string that is exactly
`${<name>}` takes the value's JSON type, and anywhere else it is interpolated as text. `${today}`, `${today-3d}`
and `${today+3d}` are UTC dates (`yyyy-MM-dd`), and `${now}` is the current UTC time in ISO 8601. An unknown
binding makes the step an `error`.

A projection may not have caught up when the next query reads it. A query step therefore reads again every
100 ms until its expectation matches or `within` milliseconds pass (default 5000, clamped to 0–30000), and
reports the number of `attempts`. `note` (up to 1000 characters) and `evidence` (an array of strings) are
accepted on journeys and steps, ignored by the replay and echoed in the report, so you can point back at the
recording a step came from.

HTTP 200 means the replay completed; `passed` tells whether every step did. A step that fails stops its journey:
later steps are `skipped`, because they would build on state the failed step did not create, while the other
journeys still run. With the file above and a model that requires unique project names, the second registration is rejected. An
excerpt of the report, showing only the failed step:

```json
{
  "schema": "stage-journey-report/v1", "passed": false, "isolation": "live",
  "summary": { "journeys": 2, "journeysPassed": 1, "steps": 4, "stepsPassed": 3, "stepsFailed": 1, "stepsSkipped": 0, "stepsErrored": 0 },
  "journeys": [
    { "name": "register a project twice", "passed": false, "steps": [
      { "index": 2, "name": "again", "kind": "command", "target": "RegisterProject", "status": "failed",
        "durationMs": 12, "route": "/api/portfolio/register-project", "httpStatus": 400,
        "actual": { "outcome": "rejected", "messages": ["Project name is already in use"] },
        "mismatches": [{ "path": "$.outcome", "expected": "accepted", "actual": "rejected", "reason": "expected accepted, found rejected" }],
        "bindings": { "project": "9d2c…", "registered": "9d2c…", "other": "41f0…" } }
    ] }
  ]
}
```

A step's `status` is `passed`, `failed`, `skipped` or `error`; `error` means the step could not be executed
(unknown or ambiguous name, unknown binding, transport failure, a 30-second step timeout, or an answer that is not
JSON). A failed or errored step reports `actual`, its `expected` as written and the `mismatches`, each with a
JSON path. `bindings` lists every value bound so far, generated GUIDs included, so you can repeat the step by
hand. A reported `response` or `result` over 64 KiB is cut to a string of its first 64 KiB with
`"truncated": true`; the comparison always uses the full answer.

The replay runs against the live session: `isolation` is `live`. Every accepted command is appended to the
session's event store and stays there, so a second replay starts from a different world. Restart the container
for a clean one. A recording that registers the same identity twice fails the second time; use `$new:` to
give each replay its own identities. The replay route itself has no persona: each operation runs as the persona
it requires, as in the browser, and a step with a `persona` property is refused.

A file is refused with HTTP 400 and every problem, each with its path, in `errors`; unknown properties are
refused rather than ignored. A body over 1 MiB returns HTTP 413. A file holds at most 100 journeys, 100 steps
per journey, 500 steps in total and `input` nested at most 32 levels deep. One replay runs at a time; another
returns HTTP 409. While the session is `loading` or `unsupported` the route returns HTTP 503 with the status.
`Stage__Journeys__TimeoutSeconds` (`Stage:Journeys:TimeoutSeconds`) sets the whole-run deadline (default 120,
clamped to 1–600 seconds); when it passes, the route returns HTTP 504 without a partial report. Importing HAR
recordings is not supported yet: convert them to this schema first.

## Behind a reverse proxy

A play session is usually not exposed on its own port but reached through whatever started it — Studio, for one,
proxies `https://<studio>/api/play/<session>/…` to the container's root. The host is built for that: it honors
the standard **`X-Forwarded-Host`**, **`X-Forwarded-Proto`** and **`X-Forwarded-Prefix`** headers, and applies
them before anything else in its pipeline. Send them from the proxy and everything that derives a URL from the
request follows the public address — the OpenAPI document's `servers` entry names
`https://<studio>/api/play/<session>` instead of the internal service name, and Scalar's request builder targets
the same, so the reference works as-is under the prefix.

The headers are trusted from any caller: there is no list of known proxies to configure, because a session only
ever sits behind the proxy of whoever started it and is thrown away with it. Keep port `9090` off the public
network accordingly — the proxy is the boundary, not the host.

## Building the images locally

Both Dockerfiles expect a prebuilt `dotnet publish` output, so build them through the script rather than calling
`docker build` directly:

```bash
./dockerize.sh
```

It publishes the host and the specification runner, then builds both images from the repository root. The host
image is tagged `cratis/studio-stage:latest` by default; `STAGE_IMAGE`, `STAGE_TAG`, `SPECRUNNER_IMAGE`,
`SPECRUNNER_TAG`, `VERSION`, `COMMIT` and `PLATFORM` override the defaults.

## Published mode (preview)

Stage defaults to Sandbox mode. Opt in to Published mode with `Stage:Mode=Published` in configuration,
`STAGE_MODE=Published` in the environment, or the `--published` host argument. Mode names are case-insensitive;
unknown names and `--published` combined with explicitly configured Sandbox mode refuse startup.

Published mode requires the semantic engine and `Stage:Sandbox:ForwardedIdentity=true`. Run behind AuthProxy
so only its forwarded principal reaches Stage. Test personas are disabled even when `Stage:Sandbox:Identity`
is enabled. Warm hosting (`--warm` or `STAGE_WARM`) and the EventModel engine are refused.

OpenAPI, Scalar, the Workbench proxy, the read-model browser, model loading, semantic admission and specification
execution endpoints are not mapped. Application APIs, `/.cratis/*`, scene, route, locale, string, package and status
endpoints, static files and the application SPA fallback remain available.

The entrypoint still requires complete `PublishedOptions`: under `Stage:Published`, a persistent
`Storage:ConnectionString` (or `Storage:ConnectionStringFile`) for the `Storage:Type` engine (`MongoDB` by default,
`PostgreSql` or `MsSql`) unless `CRATIS_CHRONICLE_CONNECTION_STRING_FILE` mounts an
[external Chronicle kernel](../reference/published-applications.md#external-chronicle-kernel), `Authentication:Mode=TrustedProxy`, and explicit
`TrustedProxies` addresses or CIDR networks; plus non-wildcard, top-level `AllowedHosts`. It also requires a valid
exported application package at `Stage:Published:PackagePath` (default `/stage/package`); the mode flag alone
is not sufficient. If Chronicle credentials are supplied under `Stage:Published`, both `Chronicle:ClientId`
and `Chronicle:ClientSecret` (or `Chronicle:ClientSecretFile`) are required.

Published mode restricts forwarded-header and principal trust to the configured proxies, but does not restrict
kernel network access. It is not a complete production deployment configuration.
