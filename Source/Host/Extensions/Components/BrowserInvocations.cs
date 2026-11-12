// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Stage.Host.Invocations;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Lets an extension component invoke an ability from the browser. The request goes through the contract's allowlist, then the screen's authorization, and
/// only then the ability path, so the browser reaches nothing a model could not already reach.
/// </summary>
/// <param name="components">The <see cref="IExtensionComponentCatalog"/>.</param>
/// <param name="authorization">The <see cref="IAuthorizationService"/>.</param>
/// <param name="invocations">The <see cref="IAbilityInvocations"/>.</param>
/// <param name="ledger">The <see cref="IInvocationLedger"/>, which the outcome is read from.</param>
/// <param name="options">The <see cref="BrowserInvocationOptions"/>.</param>
public sealed class BrowserInvocations(
    IExtensionComponentCatalog components,
    IAuthorizationService authorization,
    IAbilityInvocations invocations,
    IInvocationLedger ledger,
    BrowserInvocationOptions options)
{
    /// <summary>
    /// Invokes an ability for a component.
    /// </summary>
    /// <param name="request">The <see cref="BrowserInvocation"/>.</param>
    /// <param name="user">The person using the screen.</param>
    /// <param name="cancellation">Cancels waiting for the outcome. The invocation itself carries on.</param>
    /// <returns>The <see cref="BrowserInvocationResult"/>.</returns>
    public async Task<BrowserInvocationResult> Invoke(BrowserInvocation request, ClaimsPrincipal user, CancellationToken cancellation = default)
    {
        var placement = components.Find(request.Screen, request.System, request.Component);
        if (placement is null)
        {
            return new(BrowserInvocationStatus.NotPlaced, Reason: $"{request.Screen} places no component {request.System}.{request.Component}.");
        }

        if (!placement.Invokes.Contains(request.Ability))
        {
            return new(BrowserInvocationStatus.NotAllowed, Reason: $"{request.System}.{request.Component} does not invoke {request.Ability}.");
        }

        if (!await IsAuthorized(placement, user))
        {
            return new(BrowserInvocationStatus.Forbidden, Reason: $"You are not authorized to use {request.System}.{request.Component} on {request.Screen}.");
        }

        if (string.IsNullOrWhiteSpace(request.Interaction))
        {
            return new(BrowserInvocationStatus.Refused, Reason: "An invocation from the browser needs the identity of the interaction.");
        }

        // The person is part of the cause, so one person's interaction identity can never find another person's invocation.
        var origin = new InvocationOrigin(
            InvocationOriginKind.ScreenInteraction,
            $"{request.Screen}/{request.System}.{request.Component}",
            $"{user.Identity?.Name}/{request.Interaction}");
        var admission = await invocations.Invoke(new AbilityInvocation(request.System, request.Ability, origin, request.Input));
        if (!admission.Admitted || admission.Record is null)
        {
            return new(BrowserInvocationStatus.Refused, Reason: admission.Refusal);
        }

        return await Outcome(admission.Record, cancellation);
    }

    async Task<bool> IsAuthorized(ExtensionComponentPlacement placement, ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return placement.Policy.Length == 0 || (await authorization.AuthorizeAsync(user, null, placement.Policy)).Succeeded;
    }

    async Task<BrowserInvocationResult> Outcome(InvocationRecord record, CancellationToken cancellation)
    {
        var deadline = DateTimeOffset.UtcNow + options.Wait;
        while (true)
        {
            var current = await ledger.Get(record.Identity) ?? record;
            if (current.Status is InvocationStatus.Settled or InvocationStatus.Failed or InvocationStatus.Stopped)
            {
                var outcome = current.Outcome.Length > 0 ? current.Outcome : "failed";
                return new(BrowserInvocationStatus.Settled, current.Identity, outcome, current.Output, current.Reason);
            }

            if (DateTimeOffset.UtcNow >= deadline || cancellation.IsCancellationRequested)
            {
                return new(BrowserInvocationStatus.Pending, current.Identity);
            }

            try
            {
                await Task.Delay(options.Poll, cancellation);
            }
            catch (OperationCanceledException)
            {
                return new(BrowserInvocationStatus.Pending, current.Identity);
            }
        }
    }
}
