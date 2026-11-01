---
title: Onboarding rendering
description: Look up how the Cratis renderer provisions an application from the outcomes of its own Ante, sends the onboarding emails through Herald, and what it leaves out.
---

A model's `onboarding` block says how people get into the application. An application rendered with it runs beside its own Ante, which presents sign-up and invitations, issues invitation tokens and publishes what happened. Ante sends no mail and creates nothing in the application, so the rendered application acts on its outcomes. This is provisioning for the application's own model only.

## What the renderer emits

The files below are added to the application, in the namespace `<Root>.GeneratedOnboarding`.

| File | What it does |
| --- | --- |
| `AnteOutcomes.cs` | A reactor on `InvitationTokenIssued`, `InvitationRejected`, `InvitationToJoinTenantAccepted`, `InvitationToCreateTenantAccepted`, `OrganizationRegistrationCompleted`, `OrganizationRegistrationDeclined` and `LegalTermsAccepted` from the Ante store `Ante`. |
| `Provisioning.cs` | Creates the tenant and records members, once per outcome. |
| `OnboardingEmails.cs` | Sends the invitation, welcome and sign-up declined emails. |
| `OnboardingInvitations.cs` | The command that invites a member, and the policy that lets the personas `invite member … by` send it. Only rendered when the model names inviters. |
| `InvitationPublisher.cs` | A reactor that appends the invitation to the application's outbox for Ante, once per invitation. |
| `MemberPersonaClaims.cs` | Gives a signed-in member the roles and claims of the personas they hold in the tenant AuthProxy resolved. |
| `OnboardingRules.cs` | The personas an invitation may grant and who may send it. |
| `OnboardingEvents.cs` | The application's own events: tenant provisioned, member provisioned, legal terms accepted, email queued. |
| `OnboardingRegistration.cs` | Registers Herald, Box Office when there is an offering, the reactor, the services and the policy. |
| `OnboardingEnvironment.cs` | Reads the environment values. |
| `OnboardingOffering.cs` | Starts a new tenant on the default offering through Box Office. Only rendered when the model names an `offering`. |
| `Emails/` | The templates and images of the model, laid out the way Herald reads them. |
| `.runtime/ante.onboarding.json` | The settings of Ante, in the shape of its `Ante:Onboarding` contract. |

The project references `Cratis.Ante.Contracts` and `Cratis.Showrunner.Herald`, and `Cratis.Showrunner.BoxOffice` when the model names an `offering`. Herald reads the event store name from the client options, which an ASP.NET Core host leaves empty, so `OnboardingRegistration` sets it to the application's store. `Cratis.Ante.Contracts` requires a newer Chronicle client than the other renderings pin, so an application with onboarding pins that client and the matching kernel image in `docker-compose.yml`; every other application is unchanged.

## Provisioning

Ante's `Accepted` means published to its outbox, not provisioned in the application. Every outcome is therefore recorded with `AppendOnce`, under an idempotency key made of the outcome and the invitation or registration id, so a delivery that arrives again provisions nothing twice.

- `InvitationToCreateTenantAccepted` and `OrganizationRegistrationCompleted` create the tenant, which is the Chronicle namespace named by the tenant, and record its first member. The member holds the persona of `invite tenant as <Persona>`; a sign-up uses the same persona.
- `InvitationToJoinTenantAccepted` records a member with the personas the invitation granted.
- `LegalTermsAccepted` records the version of the terms that was accepted.

The personas granted are checked against what the model allows: an invitation to create a tenant can give only its `as` persona and an invitation to join only the `grant` personas. Ante enforces the same list; the application does it again, and the command that invites a member rejects a persona outside `grant` before anything is published. `MemberPersonaClaims` turns the personas of a member into the roles and claims the policies of the model check.

## Invitations

`invite member … by A, B` renders `InviteMemberToTenant(Email, Personas)`. A caller who is persona A or B can send it. The command records `OnboardingMemberInvited` (the address is personal data) in the tenant of the caller, under a new invitation id, because a command cannot append to another namespace while read protection is on. `InvitationPublisher` then appends `UserInvitedToJoinTenant` under the same id to the application's outbox in the default namespace, which is where Ante observes it.

An invitation to create a tenant names no inviting persona, so no command is rendered for it (`STAGE-ONBOARDING-002`); the operator invites a tenant by appending `UserInvitedToCreateTenant` to the outbox.

## Emails

The three email kinds map to the Herald kinds `invitation`, `welcome` and `signup-declined`. Each template is read from the model, given a subject when it has no header, and written as `Emails/<kind>.<language>.html` next to a manifest `Emails/<kind>.json` that declares the values the application passes:

| Kind | Values |
| --- | --- |
| `invitation` | `invitation.link` (required), `invitation.expiresAt`, `invitation.tenant`, `application.name` |
| `welcome` | `member.name`, `tenant.name`, `application.name`, `application.link` |
| `signup-declined` | `application.name`, `decline.message` |

The language is the default locale of the model's strings, otherwise `en`. Herald refuses a template that uses a value the manifest does not declare. Images are copied from the model's `images` folder to `Emails/images`; refer to them with `cid:<file>`.

The invitation link is `/invite/{token}` on the public origin of AuthProxy, read from `Cratis:Application:PublicBaseUrl`. The sender and the mail server come from the environment keys `onboarding.email.sender` and `onboarding.email.server.host`, `.port`, `.username` and `.password`; the password is a secret. When `CRATIS_MAIL_PICKUP_FOLDER` is set, Herald writes `.eml` files there instead of sending.

Each kind is sent once per invitation or registration, Herald records what it queued, and a marker records the request. An invitation whose token has an unknown or past expiry is not announced. The sign-up declined email is sent when Ante publishes `OrganizationRegistrationDeclined` (Ante 1.6.0): to the address of the sign-up, with the reviewer's message as `decline.message`, once per registration. A sign-up without an address is not answered.

Ante publishes that outcome only when sign-up review is on (`Ante:Registration:Review`). The Screenplay model has no syntax for review yet, so a rendered application leaves it off and `.runtime/ante.onboarding.json` does not mention it. A host that turns it on gets `OrganizationRegistrationAwaitingReview` in its inbox and has to decide, by appending `OrganizationRegistrationApproved` or `OrganizationRegistrationDeclineRequested(Message)` to its outbox under the registration id. Rendering those commands is not done. An invitation Ante rejects, because it asks for a persona or an invitation type the model does not allow, is a mistake in the invitation rather than a decision about the person. The application records it as `OnboardingInvitationRejected` on the invitation in the default namespace and logs a warning for an administrator; the invitee is not emailed.

## Offering

`offering <Name>` renders the product's side of Box Office. The project references `Cratis.Showrunner.BoxOffice`, which brings `Cratis.Showrunner.BoxOffice.Offering` and supplies the offers the operator published; the offering alone cannot look the default offer up. `OnboardingRegistration` then registers:

```csharp
builder.AddBoxOffice();
builder.Services.AddBoxOfficeOffering(offering => offering
    .Product(Guid.Parse("<derived from the application name>"))
    .DefaultOffering("<Name>"));
```

- **Product.** The product id is a GUID derived from the application name, so every render of the same application declares the same product. No module is declared: the modules of a Screenplay model are not Box Office modules, so nothing is gated. Declare modules in code if the product sells them.
- **Starting a tenant.** After `Provisioning` creates a tenant, from an accepted tenant invitation or a completed registration, it calls `TenantOffering.Start`, which calls `IDefaultOffering.StartTenant(tenant, organization)`. The tenant id is the name of the tenant's Chronicle namespace, which is what Box Office expects. The organization id is a GUID derived from the application and tenant name, because a self-service product has no operator-assigned organization; the same tenant is always the same organization. `StartTenant` is idempotent, so a delivery that arrives again starts nothing twice.
- **Catalog not there yet.** The operator publishes its catalog after the application starts. When `StartTenant` throws `DefaultOfferingNotPublished` (or `DefaultOfferingNotDeclared`) the error is logged and provisioning goes on: a missing offer does not stop a person getting into their tenant. That tenant is started on the offering the next time its outcome is delivered, for example after replaying the observer. Any other failure, such as `TenantNotStarted`, fails the delivery so it is retried.
- **Operator.** `AddBoxOffice` uses its defaults, so the application honors the operator event store `Direct`. Configure `OperatorStores` in code to use another one.
- **Policies.** Box Office's `BoxOffice.ManageSeats` and `BoxOffice.ManageCompute` authorization policies are not rendered, so its seat and compute commands are unavailable until the host defines them.

`STAGE-ONBOARDING-008` reports this at render time.

## What is not rendered

- **Legal documents** stay with Ante. The settings name them by their path in the model; the runtime mounts them. Ante presents two documents, terms first and privacy second; another count is reported as `STAGE-ONBOARDING-004`.

## Diagnostics

Codes are permanent. `STAGE-ONBOARDING-001` (the offering is not rendered) and `STAGE-ONBOARDING-007` (the declined email is rendered but not sent) are retired because both are now realized; their numbers are not reused.

| Code | Meaning |
| --- | --- |
| `STAGE-ONBOARDING-002` | A tenant invitation has no command. |
| `STAGE-ONBOARDING-003` | A sign-up creates a tenant, but no persona is named for its first member. |
| `STAGE-ONBOARDING-004` | The model does not list exactly two legal documents. |
| `STAGE-ONBOARDING-005` | An email template could not be read from the model root. |
| `STAGE-ONBOARDING-006` | An invitation to join a tenant names no inviting persona. |
| `STAGE-ONBOARDING-008` | Information: the offering is started through Box Office, so the operator must publish an offer with that name and the host defines the Box Office policies. |
| `STAGE-AUTH-001` | A provider kind AuthProxy does not know. |
| `STAGE-AUTH-002` | A persona that cannot be reduced to roles and claims. |
