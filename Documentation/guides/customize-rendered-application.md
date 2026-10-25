---
title: Customize a rendered application
description: Register services, add dependencies, and restyle a rendered Cratis application from unmanaged files that re-rendering does not replace.
---

Keep your adapters and styling separate from generated slice code by putting them in
`Customizations/` at the application root. The scaffold already looks for these files.

Scaffold files, including `Program.cs`, the project files, `Directory.Packages.props`, and the frontend shell,
belong to you after first run. Subsequent scoped plans exclude them. An application-scope plan includes them
again, so check your publisher's overwrite policy before applying one to an existing application.

When the caller plans with `RepositoryRoot = true`, the scaffold also includes `.github/workflows/build.yml` and
`.github/dependabot.yml`. They are scaffold files like the rest: edit them freely, and exclude the scaffold from later
plans to keep your edits.

| File                                | What it is for                                                    | Picked up when                     |
| ----------------------------------- | ----------------------------------------------------------------- | ---------------------------------- |
| `Customizations/Program.cs`         | Service registrations, options, and extra endpoints or middleware | The C# project compiles            |
| `Customizations/*.cs`               | Your adapters and other types                                     | The C# project compiles            |
| `Customizations/Dependencies.props` | Project references, package references, and MSBuild properties    | MSBuild evaluates the project      |
| `Customizations/styles.css`         | Font and `--cratis-*` token overrides for the frontend            | Vite builds or serves the frontend |

Every file is optional. Without them, the application builds and runs as rendered. The top-level
`Customizations/` directory is reserved: a model that would generate files there is rejected with
`STAGE-CRATIS-005`, including case variants.

## Before you start

- Render the application with an **application-scope** plan from `CratisRendering`. The hooks are part of the
  application scaffold; module, feature, and slice plans do not include `Program.cs` or the project file.
- Check that the rendered `Program.cs` calls `ConfigureServices(builder);` and `ConfigureApplication(app);`, and
  that `.frontend/main.tsx` discovers `Customizations/styles.css` with `import.meta.glob` and awaits its loader
  before rendering React. An application rendered before these hooks existed has neither; render it again first.

The rendered `Program.cs` provides the hooks:

```csharp title="Program.cs (managed)"
using Cratis.Arc.MongoDB;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.AddCratis(
    configureArcBuilder: arc => arc.WithMongoDB(),
    configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy());
ConfigureServices(builder);

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCratis();
app.MapHealthChecks("/healthz");
app.MapFallbackToFile("/index.html");
ConfigureApplication(app);

await app.RunAsync();

// Optional partial methods disappear when Customizations/Program.cs supplies no implementation.
public partial class Program
{
    static partial void ConfigureServices(WebApplicationBuilder builder);
    static partial void ConfigureApplication(WebApplication app);
}
```

`ConfigureServices` runs after `AddCratis` and before `Build`. `ConfigureApplication` runs after `UseCratis` and
the generated endpoints, and before `RunAsync`. When you do not implement a hook, the compiler removes the call.

The generated `/healthz` endpoint runs all health checks. With Chronicle **19.59.6 or later**, it reports healthy
only after the client is connected and Chronicle has successfully registered the application's artifacts,
including its read models. Wait for a healthy response before querying the generated application. Earlier
Chronicle versions can report healthy before registration completes.

## Keep the reducer convention stable

A new application explicitly initialized with `CreateNewApplicationProfile` persists
its `self` reducer convention in `.cratis/stage/rendering.json`. Keep that configuration
and have your rendering host reload it for later plans. This file is application
configuration, not a Screenplay behavior declaration or generated C# attachment.
See [the profile API](../reference/plan-from-sources.md#reducer-rendering-convention).

Applications without the input stay classic, including newly added reducers. Do not
switch an existing classic application to self mode by editing the setting or deleting
its reducer file. To move an existing read model, migrate it instead: Stage keeps the old
observer identity and plans the removal of the old class. See
[Migrating a classic read model](../reference/plan-from-sources.md#migrating-a-classic-read-model).
CLI and Studio integration for new-application selection and migration is separate;
untouched callers remain classic.

## Register an adapter and its options

The repository also contains a catalog adapter and stylesheet in `Samples/Customization/Customizations/`, ready
to copy into a rendered application.

This example adds a typed `HttpClient` for an external exchange-rate service, binds its base address from
configuration, and exposes one read endpoint. It uses only the ASP.NET Core shared framework, so it needs no extra
package.

Create the options type:

```csharp title="Customizations/ExchangeRatesOptions.cs"
namespace Customizations;

public class ExchangeRatesOptions
{
    public const string Section = "ExchangeRates";

    public Uri? BaseAddress { get; set; }
}
```

Create the adapter. A missing rate returns `null`; any other failed response throws, so an outage does not look
like an empty result:

```csharp title="Customizations/ExchangeRatesClient.cs"
using System.Net;

namespace Customizations;

public record ExchangeRate(string Currency, decimal Rate);

public class ExchangeRatesClient(HttpClient client)
{
    public async Task<ExchangeRate?> GetRate(string currency, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync($"rates/{Uri.EscapeDataString(currency)}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExchangeRate>(cancellationToken);
    }
}
```

Implement the hooks:

```csharp title="Customizations/Program.cs"
using Customizations;
using Microsoft.Extensions.Options;

public partial class Program
{
    static partial void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<ExchangeRatesOptions>()
            .Bind(builder.Configuration.GetSection(ExchangeRatesOptions.Section))
            .Validate(options => options.BaseAddress is not null, "ExchangeRates:BaseAddress is required.")
            .ValidateOnStart();

        builder.Services.AddHttpClient<ExchangeRatesClient>((services, client) =>
            client.BaseAddress = services.GetRequiredService<IOptions<ExchangeRatesOptions>>().Value.BaseAddress);
    }

    static partial void ConfigureApplication(WebApplication app)
    {
        app.MapGet(
            "/api/customizations/exchange-rates/{currency}",
            async (string currency, ExchangeRatesClient rates, CancellationToken cancellationToken) =>
                await rates.GetRate(currency, cancellationToken) is { } rate ? Results.Ok(rate) : Results.NotFound());
    }
}
```

Keep `Customizations/Program.cs` in the global namespace, with no `namespace` declaration, and declare the class as
`public partial class Program` or `partial class Program`. The method signatures must match the managed
declarations exactly. A namespace or a changed signature fails the build with `CS0759`.

The endpoint sits under `/api` because the generated Vite development server proxies `/api` to the backend. Pick a
path that cannot collide with a route Arc generates from your model.

## Keep custom types out of generated namespaces

Put adapter types in your own namespace, such as `Customizations`, not in the global namespace or a namespace Stage generates (including `<RootNamespace>`, `<RootNamespace>.Common` and slice namespaces). The global partial `Program` hook above is the supported exception. Remove obsolete generated declarations through your publisher's managed-file cleanup rather than leaving stale siblings in those namespaces.

Generated C# files read like hand-written code: types are written by their short names, with ordinary `using` directives above the file-scoped namespace, sorted ordinally. A namespace the project's global usings already import gets no directive. Stage keeps a fully qualified `global::` name only where it cannot prove that the short name binds to the same type, for example when two imported namespaces declare the same name. Stage decides this from the whole application's generated declarations and its framework catalog. It does not scan `Customizations/` or packages added through `Dependencies.props`.

Short names are why custom types must stay out of generated namespaces. C# looks up a short name in the file's own namespace and every enclosing namespace, out to the global namespace, before it looks at any `using` directive, and a `global using` alias wins over the directives too. A hand-written type named like a framework type in the global namespace, in `<RootNamespace>` or in `<RootNamespace>.Common`, or an alias such as `global using Checksum = ...`, would silently take over the generated reference. A type in your own namespace, such as `Customizations`, encloses no generated file and cannot do that. A package that adds a colliding type to an imported namespace, or a `global using` of a namespace you add, instead causes an ambiguity error when you build; resolve the dependency conflict rather than editing managed code.

Also review extension methods when adding dependencies. A new extension method in an imported namespace can change overload resolution in generated code, even without a type-name collision. Build and run the application's specifications after changing custom dependencies.

## Supply configuration without editing `appsettings.json`

`appsettings.json` is managed. Supply adapter settings through any other ASP.NET Core configuration source, such as
environment variables:

```bash
export ExchangeRates__BaseAddress=https://rates.example.com/v1/
dotnet run
```

End the base address with `/` so the relative `rates/...` request keeps the `/v1/` segment. With the validation
above, the host refuses to start when the setting is missing instead of failing on the first request. Keep secrets
in a secret store or environment variables, not in files under `Customizations/`.

## Add project and package references

The rendered project file imports `Customizations/Dependencies.props` when it exists. Use it to move adapters into
their own project or to add a package the shared framework does not provide:

```xml title="Customizations/Dependencies.props"
<Project>
  <ItemGroup>
    <ProjectReference Include="$(MSBuildThisFileDirectory)../../Adapters/Adapters.csproj" />
  </ItemGroup>
</Project>
```

Replace the path with your own project. `$(MSBuildThisFileDirectory)` anchors the path to the `Customizations/`
folder, so this example points at an `Adapters` folder next to the rendered application.

New scaffolds enable central package management and transitive pinning in `Directory.Packages.props`.
Add a `PackageVersion` there and use an unversioned `PackageReference` in your import. To keep an adapter's pin
entirely inside `Customizations/Dependencies.props`, use `VersionOverride` instead of `Version`:

```xml title="Customizations/Dependencies.props (package reference shape)"
<Project>
  <ItemGroup>
    <PackageReference Include="Your.Package" VersionOverride="1.2.3" />
  </ItemGroup>
</Project>
```

Replace the illustrative package name and version with your dependency. `Version` on a `PackageReference`
fails restore under central package management. Older inline-spec scaffolds that disable central package
management still require `Version` instead; check your existing `Directory.Packages.props` before adding a package.

The import is the last element of the web project body: a property you set in this file replaces the generated
value, so set only what you mean to change. Specifications compile into the same project in Debug,
so they see these dependencies too.

## Restyle the frontend

The managed `.frontend/index.css` imports `@cratis/components/tokens`, then `@cratis/components/styles`, then sets
the default font. It does not import `@cratis/components/theme`. `.frontend/main.tsx` then loads
`Customizations/styles.css` through an awaited lazy import when the file exists. React does not render until that
import completes, so your declarations load after the defaults.

Override the root font and the `--cratis-*` tokens your product needs:

```css title="Customizations/styles.css"
:root {
    font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
    font-size: 16px;

    --cratis-primary-color: #0f766e;
    --cratis-primary-color-text: #ffffff;
    --cratis-action-background: #0f766e;
    --cratis-action-background-hover: #115e59;
    --cratis-action-background-active: #134e4a;
    --cratis-action-text: #ffffff;
    --cratis-focus-ring: 0 0 0 3px #0f766e;
    --cratis-border-radius: 4px;
}
```

To map a larger design system, follow [Build a product theme](/components/styling/themed/) and check token names
in the [Cratis token reference](/components/styling/cratis-tokens/). The pinned Components version defines the
token names; a name it does not use has no effect.

The file is plain CSS processed by Vite. It can `@import` other CSS files beside it with a relative path. It cannot
add npm dependencies: `package.json` is managed, so a design-system package that is not already installed is not
available. After you create or delete `Customizations/styles.css`, restart `npm run dev` so Vite re-evaluates the
import.

## Re-render without losing your files

A Stage plan never contains a path under `Customizations/`. Stage also does not read your files: it cannot report a
broken customization, and it produces the same managed bytes whether or not the folder exists. A mistake shows up
when you build or run the application.

`CratisRendering` does not write files. Whether `Customizations/` survives a render depends on the caller that publishes
the plan, such as the Cratis CLI or Studio. A publisher that writes only the planned paths leaves your files alone.
A publisher that deletes files it did not plan, or that replaces the whole output folder, removes them. Keep
`Customizations/` under version control and check your publisher's behavior before rendering over an application
that has one.

## What the hooks do not do

- **Hooks do not bypass model admission.** Event-triggered Automation slices render their reactions and own artifacts;
  captures, non-event triggers, opaque reaction bodies and Translate slices still fail `STAGE-ESM-024`.
  Registering a service does not bind it to a modeled `file` realization or make an unsupported construct renderable.
  See [Event reaction rendering](../reference/reaction-rendering.md) for the supported shape.
- **No authorization is applied to your code.** Endpoints you map are anonymous unless you secure them with ASP.NET
  Core authorization yourself. Modeled authorization covers only the artifacts Stage renders.
- **Middleware order is fixed.** `ConfigureApplication` runs last, so middleware you add there runs after the
  generated middleware. You cannot place middleware before static files or Cratis through this hook.
- **Registrations can replace Cratis services.** `ConfigureServices` runs after `AddCratis`, and the last
  registration of a service wins. Register your own abstractions; do not re-register Cratis types unless you intend
  to replace them.
- **The frontend shell and screens stay managed.** `Customizations/styles.css` changes styling only. Screens come
  from `scene.json` and the generated bindings.
