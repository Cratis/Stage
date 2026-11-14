---
title: Authenticate a rendered application with Microsoft Entra ID
description: Opt a generated Stage host into single-tenant Entra browser and API authentication without changing model authorization.
---

A rendered application normally keeps its existing authentication behavior. To require a
Microsoft Entra ID identity at the host boundary, explicitly enable the generated host
configuration. This is independent of Screenplay personas, policies, and authentication
provider declarations: enabling Entra does not compile those declarations or map them to claims.

## Before you start

Render an **application-scope** scaffold that includes `GeneratedHostAuthentication.cs`,
`GeneratedHostAuthentication.Configuration.cs`, and `GeneratedHostAuthentication.Perimeter.cs`.
The generated `Program.cs` registers these services after `AddCratis` and installs their middleware
before static files and `UseCratis`. Keep that order when customizing the host.

Use one tenant-specific Entra application registration with:

- A browser redirect URI of `https://your-public-host/signin-oidc`, configured as a web application.
- An API audience matching the `aud` claim of the access tokens your API receives.
- Delegated API permissions or application roles for the callers that need them.
- A certificate or federated client assertion for confidential-client code redemption.

Do not use `common`, `organizations`, or `consumers` as the tenant. Client secrets, passwords,
and shared-secret client assertions are not supported. The Microsoft.Identity.Web dependency
is pinned by the scaffold, together with its cryptography dependencies.

## Configure the host explicitly

Supply these keys through the application's normal ASP.NET Core configuration providers.
Replace the angle-bracket placeholders with your registration and deployment values:

```json
{
  "Stage": {
    "Authentication": {
      "PublicOrigin": "https://app.example.test",
      "EntraId": {
        "Enabled": true,
        "TenantId": "<tenant-guid>",
        "ClientId": "<application-client-guid>",
        "Audience": "<expected-access-token-audience>",
        "ClientCredentials": [
          {
            "SourceType": "StoreWithThumbprint",
            "CertificateStorePath": "CurrentUser/My",
            "CertificateThumbprint": "<configured-certificate-thumbprint>"
          }
        ]
      }
    }
  }
}
```

`PublicOrigin` is an HTTPS origin, not a URL path: scheme, host, and optional port only.
It is the sole browser-origin allowlist and the source of the OIDC callback URL. Incoming
`Host` and forwarded headers do not choose the callback. Configure proxy routing and HTTPS
termination so browsers actually use that origin; do not add wildcard or request-derived origins.

The certificate must be available to the host identity and suitable for the application's
Entra client assertion. Provision it through your deployment's certificate mechanism, not
a model, generated source file, or committed configuration value. Exactly one credential is
supported. `StoreWithThumbprint` requires a 40-digit hexadecimal thumbprint and store path
`CurrentUser/My` or `LocalMachine/My`. Alternatively, use `SignedAssertionFromManagedIdentity`
with an optional `ManagedIdentityClientId` GUID for a user-assigned identity. Other credential
sources and unrecognized credential fields are rejected.

With `Enabled` absent or `false`, the helper registers no schemes or perimeter and does not
validate unused Entra values. With it `true`, missing or invalid tenant, client, audience,
public origin, or unsupported credentials fail startup instead of silently enabling anonymous
access. This validates configuration shape; inaccessible certificates and rejected assertions
still fail when used. The shipped `appsettings.json` contains no authentication opt-in.

Environment variables use double underscores, for example
`Stage__Authentication__EntraId__Enabled` and `Stage__Authentication__PublicOrigin`.
Do not put credential material into shell commands or logs.

## Browser and API behavior

For a top-level HTML GET navigation, an unauthenticated browser is redirected to Entra
using authorization code flow with PKCE. `/signin-oidc` is the exact protocol callback;
correlation, state, nonce, token signature, issuer, audience, and lifetime remain framework
validated. Only a successful protocol exchange creates the generated session cookie.
The cookie is HttpOnly, Secure, SameSite=Lax, and expires after eight hours without sliding
renewal. There is no generated remote sign-out endpoint.

API, discovery, XHR/fetch, and WebSocket failures return HTTP 401 without redirecting to
an HTML login page. **Any** `Authorization` header selects bearer authentication, including
an empty, malformed, or non-bearer header. A failed bearer never falls back to a valid browser
cookie. Bearer tokens must have the configured audience and tenant, a valid issuer, signature,
and lifetime, and either delegated scope `access_as_user` or application roles with `idtyp=app`. Origin is not required for valid
bearer requests; cookie write restrictions do not apply to them.

The host perimeter also protects static files, health checks, customization endpoints,
`/.cratis` and its descendants, and endpoints marked `[AllowAnonymous]`. The exact OIDC
callback is the protocol exception, not its siblings, prefixes, or unused sign-out paths.
When the host is disabled, existing `[AllowAnonymous]` behavior is unchanged. When enabled,
it only means “no additional slice-level requirement,” not “bypass the host.”

## Cookie writes and WebSockets

Every cookie-authenticated method except GET, HEAD, and OPTIONS requires exactly one
`Origin` matching `PublicOrigin`. Missing, opaque (`null`), malformed, multiple, or foreign
origins return HTTP 403 before endpoint dispatch. Scheme, host, and effective port must
match; HTTPS default port 443 is equivalent to omitting it. Host casing is insignificant.
Safe HTTP methods must remain side-effect free.

Cookie-authenticated WebSocket upgrades require the same Origin check even though their
handshake uses GET. The generated host installs WebSocket detection before the perimeter,
so a foreign or missing Origin cannot reach an observable-query socket. SameSite cookies,
CORS, and Fetch Metadata do not replace this check.

## Keep authorization separate

Host authentication supplies an identity; it does not grant application roles or translate
Screenplay persona-to-policy mappings. Existing command and query authorization still runs
after the host boundary. Arrange Entra claims and application-owned authorization separately.
Renderer diagnostics continue to report unrendered personas and authentication declarations.

For service registration and other host changes, see
[Customize a rendered application](customize-rendered-application.md).
