// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.DependencyInjection;
using Microsoft.AspNetCore.Authorization;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Checks that the identity answering an invocation may answer it.
/// </summary>
public interface IInvocationAnswerAuthorizer
{
    /// <summary>
    /// Checks an identity against the persona or policy of an invocation.
    /// </summary>
    /// <param name="record">The invocation being answered.</param>
    /// <param name="principal">The identity that answers.</param>
    /// <returns>True when it may answer.</returns>
    Task<bool> Authorize(InvocationRecord record, ClaimsPrincipal principal);
}

/// <summary>
/// Checks the policy an invocation names with the host's authorization. An invocation that names none may be answered by any authenticated identity.
/// </summary>
/// <param name="authorization">The authorization service.</param>
[IgnoreConvention]
public sealed class PolicyInvocationAnswerAuthorizer(IAuthorizationService authorization) : IInvocationAnswerAuthorizer
{
    /// <inheritdoc/>
    public async Task<bool> Authorize(InvocationRecord record, ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return false;

        // A backend answers the invocations of its own system. It is a system, not a persona, so the policy meant for people does not apply to it.
        if (principal.Identities.OfType<Extensions.ExtensionIdentity>().FirstOrDefault() is { } extension)
        {
            return AbilityReference.TryParse(record.Capability.Split('@')[0], out var system, out _) && string.Equals(system, extension.System, StringComparison.Ordinal);
        }

        if (record.AnswerPolicy.Length == 0) return true;
        return (await authorization.AuthorizeAsync(principal, record.AnswerPolicy)).Succeeded;
    }
}
