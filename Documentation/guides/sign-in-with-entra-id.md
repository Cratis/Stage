---
title: Cratis Stage sign-in with Microsoft Entra ID
description: Configure Microsoft Entra ID in a rendered Cratis host to require browser sessions or bearer tokens, with only GET and HEAD health probes exempt.
---

Use Microsoft Entra ID when a Cratis Stage-rendered application needs your organization's sign-in. Without
sign-in configuration, anyone who can reach the host can call its commands and queries. The rendered host already
contains the authentication code; you enable it through `Cratis:Authentication`, without changing the model or
editing managed files such as `appsettings.json`. Older renders need the host calls listed under
[Before you start](#before-you-start).

Browser page loads redirect to Entra ID and return to the requested page with a cookie session. Service callers
send bearer tokens: any request with an `Authorization` header uses bearer authentication and never falls back
to the cookie session.

Sign-in covers the Arc routes under `/.cratis/`, your own endpoints, static files and the single-page application
(SPA) fallback, even endpoints that allow anonymous access. Only the [health probe](#what-stays-reachable-without-signing-in)
is exempt. Cookie requests that change state and WebSocket upgrades also need a matching `Origin` header, which
identifies the browser page's origin. The host rejects a missing or different origin.

Start with an application you already render and run. [Customize a rendered application](customize-rendered-application.md)
explains where configuration and your own code belong.

## Before you start

- An Entra ID app registration for the application, with a credential the host can use to redeem the sign-in code. A
  certificate or a federated managed-identity credential is preferred; a client secret works but is discouraged.
- The exact origin browsers use to reach the application: scheme, host and, if it is not the default, the port.
- A rendered `Program.cs` that calls `<RootNamespace>.GeneratedAuthentication.Authentication.Add(builder);` before
  `ConfigureServices(builder);` and `<RootNamespace>.GeneratedAuthentication.Authentication.Use(app);` right after
  `builder.Build()`, where `<RootNamespace>` is your application's root namespace. An application rendered before this
  feature does not have the two calls; render it again or add them. See the listing in
  [Customize a rendered application](customize-rendered-application.md#before-you-start).

## Configure the keys

Sign-in is on when the `Cratis:Authentication:EntraId` section or `Cratis:Authentication:PublicOrigin` exists. With
neither present it is off: the host registers no authentication services and adds nothing to the pipeline.

| Key                                                | Required | What it sets                                                                                                     |
| -------------------------------------------------- | -------- | ---------------------------------------------------------------------------------------------------------------- |
| `Cratis:Authentication:PublicOrigin`               | Yes      | The exact origin browsers use. Scheme, host and optional port only. Redirect URIs and the Origin check use it.   |
| `Cratis:Authentication:EntraId:TenantId`           | Yes      | The directory (tenant) the application signs users in against.                                                  |
| `Cratis:Authentication:EntraId:ClientId`           | Yes      | The application (client) the app registration identifies.                                                        |
| `Cratis:Authentication:EntraId:ClientCredentials`  | Yes      | One or more credentials for the confidential client. Certificate or federated managed identity preferred.        |
| `Cratis:Authentication:EntraId:Instance`           | No       | Bound by Microsoft.Identity.Web; see its documentation.                                                          |
| `Cratis:Authentication:EntraId:Audience`           | No       | Bound by Microsoft.Identity.Web; see its documentation.                                                          |
| `Cratis:Authentication:EntraId:CallbackPath`       | No       | The sign-in callback path. The host appends it to `PublicOrigin` to build the sign-in redirect URI.              |
| `Cratis:Authentication:EntraId:SignedOutCallbackPath` | No    | The sign-out callback path. The host appends it to `PublicOrigin` to build the post-logout redirect URI.         |

The `EntraId` section is bound by Microsoft.Identity.Web, so its other options and the shape of each
`ClientCredentials` entry follow that library's documentation. The host checks only the keys marked required.

Supply them through environment variables, user secrets or your deployment's secret store. Do not put them in
`appsettings.json`, which is managed. In an environment variable, a colon in the key
becomes a double underscore, so `Cratis:Authentication:PublicOrigin` is read from `Cratis__Authentication__PublicOrigin`.
A list entry such as a credential adds its index as another segment.

Never commit a client secret, and keep it out of files under `Customizations/`.

### Register the redirect URI

In the app registration, add a redirect URI made of `Cratis:Authentication:PublicOrigin` followed by the callback
path. Do the same for the post-logout redirect, from the public origin and the signed-out callback path. The host
builds every redirect URI from `PublicOrigin` and never from the `Host` header, so the registration and the
configuration must agree exactly, including the scheme and the port.

### What a wrong configuration does

The host fails closed. If the configuration is partial or invalid, startup throws an
`InvalidAuthenticationConfiguration` exception that names the key and never prints its value. It throws when:

- only one of `Cratis:Authentication:EntraId` and `Cratis:Authentication:PublicOrigin` exists,
- `TenantId` or `ClientId` is empty,
- `ClientCredentials` has no entry, or
- `PublicOrigin` is not an absolute origin. It must be `https` and carry no user information, path, query or
  fragment. Plain `http` is accepted only for loopback hosts.

Fix the named key and start again; the application does not start unprotected.

## What stays reachable without signing in

Only `GET` and `HEAD` requests to `/healthz`, the route the host maps for health checks, skip sign-in. The response is
the status text only, because the host maps the route with ASP.NET Core's default health check response. Every other request needs a
session or a token, including:

- `POST` and every other method on `/healthz`,
- every route under `/.cratis/`,
- endpoints that allow anonymous access, and
- the static files and the SPA fallback, so the application is unreachable until the user signs in.

There is no setting that adds routes to the exemption.

## How a request is handled

The checks run right after the host is built, before static files, `UseCratis` and anything you add in
`ConfigureApplication`. Your own endpoints are covered too. In order:

1. **Authentication.** The sign-in callback completes here, before the Origin check.
2. **Origin check**, cookie requests only. A request that changes state (any method except `GET`, `HEAD`, `OPTIONS` and
   `TRACE`) or upgrades to a WebSocket must send an `Origin` header equal to `PublicOrigin` by scheme, host and port. A
   missing or different one gets `403`, even for a signed-in user. A sibling subdomain does not match. Requests with an
   `Authorization` header skip the check.
3. **Perimeter.** A signed-in request, or a health probe, continues. Otherwise:

   | Request                                                           | Result                                                            |
   | ----------------------------------------------------------------- | ----------------------------------------------------------------- |
   | Has an `Authorization` header                                     | `401` bearer challenge                                            |
   | `GET` or `HEAD` page navigation                                   | Redirect to Entra ID, then back to the same path and query        |
   | Anything else, such as a `fetch` from the SPA after session expiry | `401` with no redirect                                           |

   A request counts as a navigation when `Sec-Fetch-Mode` is `navigate`, or when that header is absent and `Accept`
   contains `text/html`. The return path must be a local path; anything else returns to `/`.
4. **Authorization.** Arc's policies run as before. Sign-in decides who is authenticated; it does not change what an
   authenticated user may do.

The session cookie is `HttpOnly`, always `Secure` and `SameSite=Lax`. The sign-in correlation and nonce cookies are
`Secure` too, so sign-in works behind a proxy that terminates TLS without forwarded headers.

## Run it locally

The host always marks its cookies `Secure`, and it accepts a plain `http` `PublicOrigin` only for loopback hosts. Check
how your browser handles `Secure` cookies over `http` on `localhost`, or run the host over `https`.

1. Set `Cratis:Authentication:PublicOrigin` to the origin your browser actually uses, not the origin of the process
   behind it. That is the host's own origin when it serves the built frontend, or the Vite dev server's origin when
   you work on the frontend.
2. Register that origin plus the callback path as a redirect URI in the app registration.
3. With the Vite dev server, forward the callback paths to the backend. The rendered `.frontend/vite.config.ts` proxies
   `/.cratis`, `/api` and `/swagger` only; add the callback and signed-out callback paths to the same `proxy` block.

You know it works when opening the application signs you in through Entra ID and returns you to the page you asked
for, and when `GET /healthz` still answers without a session.

## Host more than one instance

The session cookie and the sign-in correlation cookies are protected with ASP.NET Core data protection. When several
instances serve the same application, configure data protection so they share keys, following the ASP.NET Core data
protection documentation, in `Customizations/Program.cs`. The token cache the host registers is held in memory per
instance.

## What this is not

This sign-in flow is separate from Arc's Microsoft Identity Platform (Easy Auth) header integration. The host
does not read the platform's identity headers or map Entra ID users and groups to Screenplay personas. Authentication
establishes who is signed in; it does not choose their persona or change Arc's authorization policies.

There is no additional model or render option. The Screenplay model and the managed `GeneratedAuthentication.cs`
and `GeneratedAuthenticationPerimeter.cs` stay as rendered.

## Next steps

- [Customize a rendered application](customize-rendered-application.md) covers the hooks, the `Customizations/`
  folder and supplying configuration without editing `appsettings.json`.
