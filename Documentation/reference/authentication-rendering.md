---
title: Authentication rendering
description: Look up how the Cratis renderer makes an application trust the identity AuthProxy forwards, which files it renders for a runtime to start AuthProxy with, and how the sandbox follows.
---

A model's `authentication` block names the identity providers an application signs people in with. The Cratis renderer turns it into an application that runs behind Cratis AuthProxy and a configuration AuthProxy starts from.

## What the renderer emits

For a model that declares

```screenplay
authentication
  provider EntraId
  provider GitHub
  provider OpenId name Partner
```

an application plan adds the following to the application:

| File | What it is |
| --- | --- |
| `appsettings.json` | `Cratis:Arc:TrustForwardedIdentityHeaders` is `true`, so Arc builds the caller from the identity headers AuthProxy forwards. |
| `Program.cs` | Calls `RuntimeConventions.Apply` before Cratis reads its configuration. |
| `GeneratedAuthentication/RuntimeConventions.cs` | Reads the Chronicle connection string from the file named by `CRATIS_CHRONICLE_CONNECTION_STRING_FILE`. |
| `GeneratedAuthentication/Personas.cs` | The personas of the model with the roles and claims that satisfy the policies each one holds. |
| `GeneratedAuthentication/MemberIdentity.cs` | The identity of a person: the provider they sign in with and their subject. |
| `.runtime/authproxy.application.json` | The provider list in AuthProxy's `Cratis:AuthProxy:Application` shape. |
| `.runtime/environment.json` | The environment keys the model needs, with the secrets marked. |

The identity headers are not signed. Arc ignores them unless `TrustForwardedIdentityHeaders` is on, and AuthProxy removes every identity header a client sends and writes its own, so the setting is right only when every request reaches the application through AuthProxy. Never expose a rendered application with authentication directly.

The policies the commands and queries already carry need nothing else. A caller AuthProxy forwarded satisfies `require authenticated`, `require role` and `require claim` through the roles and claims in the forwarded principal; an anonymous caller satisfies none of them, and an `authorize Anyone` policy still renders as `[AllowAnonymous]`.

## The files a runtime reads

`cratis run` and Stage Manager find the runtime files in the `.runtime` folder of the rendered application and add what only they know: the environment name, the secrets directory, `Development` and the values of the environment.

```json
{
  "Cratis": {
    "AuthProxy": {
      "Application": {
        "Providers": [
          { "Kind": "EntraId", "Identity": "EntraId" },
          { "Kind": "GitHub", "Identity": "GitHub" },
          { "Kind": "OpenId", "Identity": "Partner" }
        ]
      }
    }
  }
}
```

`Onboarding` is `true` in the same file when the model declares [onboarding](onboarding-rendering.md). The kinds are `EntraId`, `GitHub`, `Google`, `Apple` and `OpenId`; a provider of another kind is reported as `STAGE-AUTH-001` and left out. `RuntimeConfigurationFiles.Render` produces the same files for a caller that holds only a realization.

The application container receives:

| Variable | Meaning |
| --- | --- |
| `CRATIS_ENVIRONMENT` | The name of the environment. |
| `CRATIS_SECRETS_DIRECTORY` | A directory with one file per environment key, named by the key. |
| `CRATIS_CHRONICLE_CONNECTION_STRING_FILE` | A file holding the Chronicle connection string. |

## Personas

A persona holds policies. `Personas.cs` records, per persona, the smallest set of roles and claims that satisfies all of its policies, in the order Screenplay's persona callers use. `Personas.Holds` is true for a caller who has all of them. A persona whose policies are not requirements on authentication, roles or claims cannot be reduced to roles and claims, is never matched and is reported as `STAGE-AUTH-002`.

## Planning

The realization is not part of the executable model, so a caller adds it to the profile:

```csharp
var profile = CratisRendering.WithRealization(CratisRendering.CreateProfile(name, options), compilation, modelRoot);
```

`compilation` is the `SemanticCompilation` the plan is made from. `modelRoot` is the folder holding the `.play` files, from which the images of onboarding emails are read; leave it out when there are none. `SemanticModelLoader` does the same for `LoadedSemanticModel.Realization`, and `CratisRendering.PlanFrom` adds it itself. The realization is a profile input, so it is part of the input digest.

## The sandbox

`cratis run` starts the Stage sandbox behind AuthProxy when the model has authentication. Set `Stage:Sandbox:ForwardedIdentity` (environment variable `Stage__Sandbox__ForwardedIdentity`) to `true` and the sandbox trusts the forwarded identity exactly as a rendered application does: it plays no persona and the signed-in caller is the caller its authorization runs for. Without the setting, the sandbox keeps playing a persona and removes the identity headers a caller sends.

## The legacy writer

The syntax-based writer that `IRenderer` exposes writes `.runtime/authproxy.application.json` and `.runtime/environment.json`, and reports a provider kind AuthProxy does not know. It does not change the scaffold, so the application it writes does not trust the forwarded identity; use `cratis render` for that.
