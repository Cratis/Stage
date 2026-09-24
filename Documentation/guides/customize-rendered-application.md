---
title: Customize a rendered application
description: Register services, add dependencies, and restyle a rendered Cratis application from unmanaged files that re-rendering does not replace.
---

A rendered Cratis application owns its `Program.cs`, project file, `appsettings.json`, and frontend shell. Every
application render plans those files again, so an edit to them is lost or conflicts on the next render. Put your own code in a
`Customizations/` folder at the application root instead. The generated files already look for it.

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

The rendered application disables central package management, so a `PackageReference` here needs an explicit
`Version`. The import is the last element of the project body: a property you set in this file replaces the
generated value, so set only what you mean to change.

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

- **Modeled captures, reactions, and `file` realizations are still unsupported.** A service you register is not
  bound to any Screenplay construct, and planner admission is unchanged: a selected slice the planner does not
  support, such as an Automation or Translation slice, still blocks the plan with `STAGE-ESM-001`. Stage continues
  to pin Screenplay 4.17.0.
- **No authorization is applied to your code.** Endpoints you map are anonymous unless you secure them with ASP.NET
  Core authorization yourself. Modeled authorization covers only the artifacts Stage renders.
- **Middleware order is fixed.** `ConfigureApplication` runs last, so middleware you add there runs after the
  generated middleware. You cannot place middleware before static files or Cratis through this hook.
- **Registrations can replace Cratis services.** `ConfigureServices` runs after `AddCratis`, and the last
  registration of a service wins. Register your own abstractions; do not re-register Cratis types unless you intend
  to replace them.
- **The frontend shell and screens stay managed.** `Customizations/styles.css` changes styling only. Screens come
  from `scene.json` and the generated bindings.
