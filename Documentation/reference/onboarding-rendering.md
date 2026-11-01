---
title: Onboarding rendering
description: Look up how the Cratis renderer provisions an application from the outcomes of its own Ante, sends the onboarding emails through Herald, and what it leaves out.
---

A model's `onboarding` block says how people get into the application. An application rendered with it runs beside its own Ante, which presents sign-up and invitations, issues invitation tokens and publishes what happened. Ante sends no mail and creates nothing in the application, so the rendered application acts on its outcomes. This is provisioning for the application's own model only.

## What the renderer emits

The files below are added to the application, in the namespace `<Root>.GeneratedOnboarding`.

| File | What it does |
| --- | --- |
| `AnteOutcomes.cs` | A reactor on `InvitationTokenIssued`, `InvitationRejected`, `InvitationToJoinTenantAccepted`, `InvitationToCreateTenantAccepted`, `OrganizationRegistrationCompleted` and `LegalTermsAccepted` from the Ante store `Ante`. |
| `Provisioning.cs` | Creates the tenant and records members, once per outcome. |
| `OnboardingEmails.cs` | Sends the invitation and welcome emails. |
| `OnboardingInvitations.cs` | The command that invites a member, and the policy that lets the personas `invite member … by` send it. Only rendered when the model names inviters. |
| `InvitationPublisher.cs` | A reactor that appends the invitation to the application's outbox for Ante, once per invitation. |
| `MemberPersonaClaims.cs` | Gives a signed-in member the roles and claims of the personas they hold in the tenant AuthProxy resolved. |
| `OnboardingRules.cs` | The personas an invitation may grant and who may send it. |
| `OnboardingEvents.cs` | The application's own events: tenant provisioned, member provisioned, legal terms accepted, email queued. |
| `OnboardingRegistration.cs` | Registers Herald, the reactor, the services and the policy. |
| `OnboardingEnvironment.cs` | Reads the environment values. |
| `Emails/` | The templates and images of the model, laid out the way Herald reads them. |
| `.runtime/ante.onboarding.json` | The settings of Ante, in the shape of its `Ante:Onboarding` contract. |

The project references `Cratis.Ante.Contracts` and `Cratis.Showrunner.Herald`. Herald reads the event store name from the client options, which an ASP.NET Core host leaves empty, so `OnboardingRegistration` sets it to the application's store. `Cratis.Ante.Contracts` requires a newer Chronicle client than the other renderings pin, so an application with onboarding pins that client and the matching kernel image in `docker-compose.yml`; every other application is unchanged.

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

Each kind is sent once per invitation or registration, Herald records what it queued, and a marker records the request. An invitation whose token has an unknown or past expiry is not announced. The sign-up declined email is rendered but not sent: it is for a sign-up a person reviewed and declined, and Ante does not publish that outcome yet, so the render reports `STAGE-ONBOARDING-007`. An invitation Ante rejects, because it asks for a persona or an invitation type the model does not allow, is a mistake in the invitation rather than a decision about the person. The application records it as `OnboardingInvitationRejected` on the invitation in the default namespace and logs a warning for an administrator; the invitee is not emailed.

## What is not rendered

- **Offering.** Box Office has no contract for a product starting a tenant on its own default offering without an operator's decision, so `offering` renders nothing, no Box Office package is referenced and `STAGE-ONBOARDING-001` says so. Tenants are provisioned without licensing.
- **Legal documents** stay with Ante. The settings name them by their path in the model; the runtime mounts them. Ante presents two documents, terms first and privacy second; another count is reported as `STAGE-ONBOARDING-004`.

## Diagnostics

| Code | Meaning |
| --- | --- |
| `STAGE-ONBOARDING-001` | The offering is not rendered. |
| `STAGE-ONBOARDING-002` | A tenant invitation has no command. |
| `STAGE-ONBOARDING-003` | A sign-up creates a tenant, but no persona is named for its first member. |
| `STAGE-ONBOARDING-004` | The model does not list exactly two legal documents. |
| `STAGE-ONBOARDING-005` | An email template could not be read from the model root. |
| `STAGE-ONBOARDING-006` | An invitation to join a tenant names no inviting persona. |
| `STAGE-ONBOARDING-007` | The model declares a `signupDeclined` template; it is rendered but not sent until Ante publishes a declined sign-up. |
| `STAGE-AUTH-001` | A provider kind AuthProxy does not know. |
| `STAGE-AUTH-002` | A persona that cannot be reduced to roles and claims. |
