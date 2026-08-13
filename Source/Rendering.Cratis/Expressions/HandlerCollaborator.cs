// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Expressions;

/// <summary>
/// Represents a collaborator a rendered handler takes as a parameter to reach a value the Screenplay document
/// asks for from the surrounding context.
/// </summary>
/// <param name="TypeName">The collaborator's C# type name.</param>
/// <param name="ParameterName">The parameter name the rendered expressions refer to it by.</param>
/// <param name="Namespace">The namespace to import for the type, or empty when the type name is already qualified.</param>
/// <remarks>
/// Arc resolves a command handler's parameters from dependency injection by type, so asking for a collaborator is
/// how a rendered handler reaches anything beyond the command's own properties. Each one here is a type the Cratis
/// runtime registers by convention.
/// </remarks>
public sealed record HandlerCollaborator(string TypeName, string ParameterName, string Namespace)
{
    /// <summary>
    /// Gets the collaborator giving the tenant the command executes for.
    /// </summary>
    public static readonly HandlerCollaborator Tenants = new("ITenantIdAccessor", "tenants", "Cratis.Arc.Tenancy");

    /// <summary>
    /// Gets the collaborator giving the identity recorded as having caused what the command appends.
    /// </summary>
    /// <remarks>
    /// Named in full: the Cratis package's global usings bring in both <c>Cratis.Chronicle.Identities</c> and
    /// <c>Cratis.Arc.Identity</c>, and each declares an <c>IIdentityProvider</c>, so the short name is ambiguous
    /// in every rendered file whether or not this one adds a using of its own.
    /// </remarks>
    public static readonly HandlerCollaborator Identities = new("Cratis.Chronicle.Identities.IIdentityProvider", "identities", string.Empty);

    /// <summary>
    /// Gets the collaborator giving the calling principal — what the caller can prove, rather than who they are.
    /// </summary>
    public static readonly HandlerCollaborator Principals = new("ICurrentPrincipalAccessor", "principals", "Cratis.Arc.Authorization");

    /// <summary>
    /// Gets the collaborator giving what caused the command to run.
    /// </summary>
    public static readonly HandlerCollaborator Causations = new("ICausationManager", "causations", "Cratis.Chronicle.Auditing");

    /// <summary>
    /// Renders the collaborator as a method parameter.
    /// </summary>
    /// <returns>The parameter declaration.</returns>
    public string ToParameter() => $"{TypeName} {ParameterName}";
}
